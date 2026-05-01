using UnityEngine;
using UnityEngine.InputSystem;

public class MopCleaner : MonoBehaviour
{
    [Header("Shader & Texture")]
    public Material mopMaterial;
    public RenderTexture bloodRT;

    [Header("Initial Blood Shape")]
    [Tooltip("Drag your whiteSplatter texture here — this defines where blood starts. " +
             "The RT is seeded from this texture at startup instead of being filled solid white.")]
    public Texture2D initialBloodMask;

    [Header("Floor Connection")]
    [Tooltip("The Renderer on your floor Plane (Plane (1)).")]
    public Renderer floorRenderer;

    [Tooltip("Reference name of the mask texture property in FloorBloodShader. " +
             "Open the shader graph → Blackboard → check the Texture2D property name. " +
             "Usually _MaskTex.")]
    public string maskTexPropertyName = "_MaskTex";

    [Header("Camera & Input")]
    public Camera playerCamera;
    public LayerMask floorMask = ~0;
    public float rayDistance = 20f;

    [Header("Brush")]
    [Range(0.005f, 0.25f)] public float brushRadius = 0.05f;
    [Range(0.002f, 0.15f)] public float brushStrength = 0.04f;

    // ── Private ────────────────────────────────────────────────────────────
    Material _mat;
    RenderTexture _temp;
    bool _painting;

    static readonly int ID_HitUV = Shader.PropertyToID("_HitUV");
    static readonly int ID_Radius = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");

    InputSystem_Actions _input;

    void Awake() { _input = new InputSystem_Actions(); }

    void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Attack.performed += _ => _painting = true;
        _input.Player.Attack.canceled += _ => _painting = false;
    }

    void OnDisable() { _input.Player.Disable(); }

    void Start()
    {
        if (!mopMaterial) { Debug.LogError("[MopCleaner] mopMaterial missing!"); enabled = false; return; }
        if (!bloodRT) { Debug.LogError("[MopCleaner] bloodRT missing!"); enabled = false; return; }
        if (!playerCamera) { Debug.LogError("[MopCleaner] playerCamera missing!"); enabled = false; return; }
        if (!floorRenderer) { Debug.LogError("[MopCleaner] floorRenderer missing!"); enabled = false; return; }

        if (initialBloodMask == null)
            Debug.LogWarning("[MopCleaner] initialBloodMask not assigned — floor will start fully clean. " +
                             "Drag your whiteSplatter texture into the Initial Blood Mask slot.");

        // Clone material — never mutate the project asset
        _mat = new Material(mopMaterial);

        // Ping-pong buffer
        _temp = new RenderTexture(bloodRT.descriptor);
        _temp.name = "MopClean_Temp";
        _temp.Create();

        // ── Seed the RT from the splat texture ─────────────────────────────
        // This copies whiteSplatter into bloodRT so only splat pixels = blood.
        // If no splat texture assigned, clear to black (no blood visible).
        var prev = RenderTexture.active;
        RenderTexture.active = bloodRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;

        if (initialBloodMask != null)
            Graphics.Blit(initialBloodMask, bloodRT);   // splat shape → RT

        // ── Connect bloodRT to the floor material's mask slot ──────────────
        floorRenderer.material.SetTexture(maskTexPropertyName, bloodRT);
        Debug.Log($"[MopCleaner] BloodRT → {floorRenderer.name} [{maskTexPropertyName}] ✓");
    }

    void OnDestroy()
    {
        if (_mat) Destroy(_mat);
        if (_temp) { _temp.Release(); Destroy(_temp); }
    }

    void Update()
    {
        if (!_painting) return;

        Ray ray = playerCamera.ScreenPointToRay(
            UnityEngine.InputSystem.Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorMask))
            Erase(hit.textureCoord);
    }

    void Erase(Vector2 uv)
    {
        _mat.SetVector(ID_HitUV, new Vector4(uv.x, uv.y, 0f, 0f));
        _mat.SetFloat(ID_Radius, brushRadius);
        _mat.SetFloat(ID_Strength, brushStrength);

        Graphics.Blit(bloodRT, _temp);
        Graphics.Blit(_temp, bloodRT, _mat);
    }

    /// <summary>Call to respawn blood at its original splat shape.</summary>
    public void ResetBlood()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = bloodRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;

        if (initialBloodMask != null)
            Graphics.Blit(initialBloodMask, bloodRT);
    }
}