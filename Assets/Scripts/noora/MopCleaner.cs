using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MopCleaner : MonoBehaviour
{
    [Header("Shader & Texture")]
    public Material mopMaterial;
    public RenderTexture bloodRT;

    [Header("Initial Blood Shape")]
    [Tooltip("Drag your whiteSplatter texture here — this defines where blood starts.")]
    public Texture2D initialBloodMask;

    [Header("Floor Connection")]
    [Tooltip("The Renderer on your floor Plane (Plane (1)).")]
    public Renderer floorRenderer;
    public string maskTexPropertyName = "_MaskTex";

    [Header("Camera & Input")]
    public Camera playerCamera;
    public LayerMask floorMask = ~0;
    public float rayDistance = 20f;

    [Header("Brush")]
    [Range(0.005f, 0.25f)] public float brushRadius = 0.05f;
    [Range(0.002f, 0.15f)] public float brushStrength = 0.04f;

    [Header("Inventory")]
    public InventorySystem inventory;

    [Header("Dirty Mop Settings")]
    [Tooltip("The Bucket GameObject sitting on the floor in the scene.")]
    public GameObject bucketObject;

    [Tooltip("How close the player must be to the bucket to auto-dip (world units).")]
    public float dipDistance = 1.5f;

    [Tooltip("How much UV distance the mop must travel while cleaning before it gets dirty. " +
             "0.5 = roughly half a texture width of mopping.")]
    public float cleanDistanceBeforeDirty = 3.0f;

    [Tooltip("Spread strength when mop is dirty (paints blood back onto the floor).")]
    [Range(0.002f, 0.15f)] public float spreadStrength = 0.03f;

    [Header("UI Feedback")]
    [Tooltip("Optional UI Text to show mop status on screen.")]
    public Text statusText;

    // ── Private ────────────────────────────────────────────────────────────
    Material _mat;
    RenderTexture _temp;
    bool _painting;

    float _cleanedDistance = 0f;
    Vector2 _lastUV = Vector2.negativeInfinity;
    bool _mopIsDirty = false;

    static readonly int ID_HitUV = Shader.PropertyToID("_HitUV");
    static readonly int ID_Radius = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");
    static readonly int ID_Spread = Shader.PropertyToID("_Spread");

    InputSystem_Actions _input;

    void Awake() { _input = new InputSystem_Actions(); }

    void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Attack.performed += _ => _painting = true;
        _input.Player.Attack.canceled += OnRelease;
    }

    void OnDisable() { _input.Player.Disable(); }

    void OnRelease(InputAction.CallbackContext _)
    {
        _painting = false;
        _lastUV = Vector2.negativeInfinity;
    }

    void Start()
    {
        if (!mopMaterial) { Debug.LogError("[MopCleaner] mopMaterial missing!"); enabled = false; return; }
        if (!bloodRT) { Debug.LogError("[MopCleaner] bloodRT missing!"); enabled = false; return; }
        if (!playerCamera) { Debug.LogError("[MopCleaner] playerCamera missing!"); enabled = false; return; }
        if (!floorRenderer) { Debug.LogError("[MopCleaner] floorRenderer missing!"); enabled = false; return; }

        if (!bucketObject)
            Debug.LogWarning("[MopCleaner] bucketObject not assigned — auto-dip won't work!");

        if (!inventory)
            inventory = GetComponent<InventorySystem>();
        if (!inventory) { Debug.LogError("[MopCleaner] InventorySystem not found!"); enabled = false; return; }

        if (initialBloodMask == null)
            Debug.LogWarning("[MopCleaner] initialBloodMask not assigned — floor starts clean.");

        _mat = new Material(mopMaterial);
        _temp = new RenderTexture(bloodRT.descriptor);
        _temp.name = "MopClean_Temp";
        _temp.Create();

        var prev = RenderTexture.active;
        RenderTexture.active = bloodRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;

        if (initialBloodMask != null)
            Graphics.Blit(initialBloodMask, bloodRT);

        floorRenderer.material.SetTexture(maskTexPropertyName, bloodRT);
        Debug.Log($"[MopCleaner] BloodRT → {floorRenderer.name} [{maskTexPropertyName}] ✓");

        UpdateStatusUI();
    }

    void OnDestroy()
    {
        if (_mat) Destroy(_mat);
        if (_temp) { _temp.Release(); Destroy(_temp); }
    }

    void Update()
    {
        // ── Dip check: runs every frame ────────────────────────────────────
        // Conditions to dip:
        //   1. Mop is dirty
        //   2. Player has selected the bucket (pressed 1)
        //   3. Player is close enough to the bucket object on the floor
        if (_mopIsDirty
            && inventory.IsBucketActive()
            && bucketObject != null
            && Vector3.Distance(transform.position, bucketObject.transform.position) <= dipDistance)
        {
            DipMop();
            return;
        }

        // ── Mopping: only when mop is the held hand-tool ───────────────────
        if (inventory.GetHandSlot() != InventorySystem.SLOT_MOP)
        {
            if (_painting) Debug.Log("[MopCleaner] Equip the Mop (press 2) to clean.");
            return;
        }

        if (!_painting) return;

        Ray ray = playerCamera.ScreenPointToRay(
            UnityEngine.InputSystem.Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, floorMask))
        {
            if (_mopIsDirty)
                ApplyBrush(hit.textureCoord, spreadStrength, spread: true);
            else
                ApplyBrush(hit.textureCoord, brushStrength, spread: false);
        }
        else
        {
            _lastUV = Vector2.negativeInfinity;
        }
    }

    // ── Core ───────────────────────────────────────────────────────────────

    void ApplyBrush(Vector2 uv, float strength, bool spread)
    {
        _mat.SetVector(ID_HitUV, new Vector4(uv.x, uv.y, 0f, 0f));
        _mat.SetFloat(ID_Radius, brushRadius);
        _mat.SetFloat(ID_Strength, strength);
        _mat.SetFloat(ID_Spread, spread ? 1f : 0f);

        Graphics.Blit(bloodRT, _temp);
        Graphics.Blit(_temp, bloodRT, _mat);

        if (!spread)
        {
            if (_lastUV.x >= 0f)
                _cleanedDistance += Vector2.Distance(uv, _lastUV);

            _lastUV = uv;
            UpdateStatusUI();

            if (_cleanedDistance >= cleanDistanceBeforeDirty)
            {
                _mopIsDirty = true;
                Debug.LogWarning("[MopCleaner] ⚠ Mop is dirty! Select bucket (1) and walk to it.");
                UpdateStatusUI();
            }
        }
    }

    void DipMop()
    {
        _mopIsDirty = false;
        _cleanedDistance = 0f;
        _lastUV = Vector2.negativeInfinity;
        Debug.Log("[MopCleaner] ✅ Mop dipped — ready to clean again!");
        UpdateStatusUI();
    }

    void UpdateStatusUI()
    {
        if (statusText == null) return;

        if (_mopIsDirty)
        {
            statusText.text = "⚠ Mop dirty! Press 1 to select bucket, then walk to it.";
        }
        else
        {
            float pct = Mathf.RoundToInt(Mathf.Clamp01(_cleanedDistance / cleanDistanceBeforeDirty) * 100f);
            statusText.text = $"Mop clean — {pct}% used";
        }
    }

    public void ResetBlood()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = bloodRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;

        if (initialBloodMask != null)
            Graphics.Blit(initialBloodMask, bloodRT);

        _cleanedDistance = 0f;
        _mopIsDirty = false;
        _lastUV = Vector2.negativeInfinity;
        UpdateStatusUI();
    }
}