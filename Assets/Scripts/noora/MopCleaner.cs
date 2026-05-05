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
    [Tooltip("Set this to ONLY the Floor layer so the raycast never hits walls or player.")]
    public LayerMask floorMask = ~0;
    public float rayDistance = 20f;
    [Tooltip("Assign the Player layer here so the raycast never hits the player.")]
    public LayerMask excludeLayers;

    [Header("Brush")]
    [Range(0.005f, 0.25f)] public float brushRadius = 0.05f;
    [Range(0.002f, 0.15f)] public float brushStrength = 0.04f;

    [Header("Mop Range")]
    [Tooltip("How close the player must be to the floor plane to mop.")]
    public float mopRange = 13f;

    [Header("Inventory")]
    public ToolInventory inventory;

    [Header("Dirty Mop Settings")]
    [Tooltip("The Bucket GameObject sitting on the floor in the scene.")]
    public GameObject bucketObject;

    [Tooltip("How close the player must be to the bucket to auto-dip (world units).")]
    public float dipDistance = 1.5f;

    [Tooltip("How much UV distance the mop must travel while cleaning before it gets dirty.")]
    public float cleanDistanceBeforeDirty = 3.0f;

    [Tooltip("Spread strength when mop is dirty (paints blood back onto the floor).")]
    [Range(0.002f, 0.15f)] public float spreadStrength = 0.03f;

    [Header("Footprint Cleaning")]
    [Tooltip("World-space radius around the raycast hit point to clean footprints.")]
    public float footprintCleanRadius = 0.4f;

    [Tooltip("How long the mop must stay over a footprint to fully clean it (seconds). " +
             "Set to 0 for instant removal.")]
    public float footprintCleanTime = 0.6f;

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

    System.Collections.Generic.Dictionary<GameObject, float> _footprintProgress
        = new System.Collections.Generic.Dictionary<GameObject, float>();

    static readonly int ID_HitUV = Shader.PropertyToID("_HitUV");
    static readonly int ID_Radius = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");
    static readonly int ID_Spread = Shader.PropertyToID("_Spread");

    InputSystem_Actions _input;

    void Awake() { _input = new InputSystem_Actions(); }

    void OnEnable()
    {
        _input.Player.Enable();
        _input.Player.Interact.started += _ => _painting = true;
        _input.Player.Interact.canceled += OnRelease;
    }

    void OnDisable() { _input.Player.Disable(); }

    void OnRelease(InputAction.CallbackContext _)
    {
        _painting = false;
        _lastUV = Vector2.negativeInfinity;
        _footprintProgress.Clear();
    }

    void Start()
    {
        if (!mopMaterial) { Debug.LogError("[MopCleaner] mopMaterial missing!"); enabled = false; return; }
        if (!bloodRT) { Debug.LogError("[MopCleaner] bloodRT missing!"); enabled = false; return; }
        if (!playerCamera) { Debug.LogError("[MopCleaner] playerCamera missing!"); enabled = false; return; }
        if (!floorRenderer) { Debug.LogError("[MopCleaner] floorRenderer missing!"); enabled = false; return; }

        if (!bucketObject)
            Debug.LogWarning("[MopCleaner] bucketObject not assigned — auto-dip won't work!");

        if (!inventory) inventory = GetComponent<ToolInventory>();
        if (!inventory) { Debug.LogError("[MopCleaner] ToolInventory not found!"); enabled = false; return; }

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
        // ── Require mop to be equipped ─────────────────────────────────────
        if (!inventory.IsMopSelected())
        {
            if (_painting) Debug.Log("[MopCleaner] Equip the Mop (press 1) to clean.");
            _painting = false;
            return;
        }

        // ── Dip check ──────────────────────────────────────────────────────
        if (_mopIsDirty && bucketObject != null)
            Debug.Log($"[Mop] Dist to bucket: {Vector3.Distance(transform.position, bucketObject.transform.position)} | dipDistance: {dipDistance}");

        if (_mopIsDirty
            && bucketObject != null
            && Vector3.Distance(transform.position, bucketObject.transform.position) <= dipDistance)
        {
            DipMop();
            return;
        }

        if (!_painting)
        {
            _footprintProgress.Clear();
            return;
        }

        // ── Range check — player must be close to floor blood ──────────────
        float distToFloor = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(floorRenderer.transform.position.x, 0f, floorRenderer.transform.position.z));

        if (distToFloor > mopRange)
        {
            _painting = false;
            _lastUV = Vector2.negativeInfinity;
            return;
        }

        // ── Raycast ────────────────────────────────────────────────────────
        LayerMask finalMask = floorMask & ~excludeLayers;

        Ray ray = playerCamera.ScreenPointToRay(
            UnityEngine.InputSystem.Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, finalMask))
        {
            // Only accept hits on the floor renderer
            if (hit.collider.gameObject != floorRenderer.gameObject)
            {
                _lastUV = Vector2.negativeInfinity;
                _footprintProgress.Clear();
                return;
            }

            if (_mopIsDirty)
                ApplyBrush(hit.textureCoord, spreadStrength, spread: true);
            else
                ApplyBrush(hit.textureCoord, brushStrength, spread: false);

            if (!_mopIsDirty)
                CleanFootprintsNear(transform.position);
        }
        else
        {
            _lastUV = Vector2.negativeInfinity;
            _footprintProgress.Clear();
        }
    }

    // ── Footprint cleaning ─────────────────────────────────────────────────

    void CleanFootprintsNear(Vector3 worldPoint)
    {
        Debug.Log($"[Mop] Cleaning near {worldPoint} | Active footprints: {FootprintTracker.ActiveFootprints.Count}");

        var nullKeys = new System.Collections.Generic.List<GameObject>();
        foreach (var key in _footprintProgress.Keys)
            if (key == null) nullKeys.Add(key);
        foreach (var key in nullKeys) _footprintProgress.Remove(key);

        bool anyInRange = false;

        var snapshot = new System.Collections.Generic.List<GameObject>(FootprintTracker.ActiveFootprints);

        foreach (GameObject fp in snapshot)
        {
            if (fp == null) continue;

            float dist = Vector3.Distance(
                new Vector3(worldPoint.x, 0f, worldPoint.z),
                new Vector3(fp.transform.position.x, 0f, fp.transform.position.z));

            if (dist > footprintCleanRadius)
            {
                _footprintProgress.Remove(fp);
                continue;
            }

            anyInRange = true;

            if (footprintCleanTime <= 0f)
            {
                FootprintTracker.RemoveFootprint(fp);
                continue;
            }

            if (!_footprintProgress.ContainsKey(fp))
                _footprintProgress[fp] = 0f;

            _footprintProgress[fp] += Time.deltaTime;

            Renderer r = fp.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                float progress = Mathf.Clamp01(_footprintProgress[fp] / footprintCleanTime);
                Color col = r.material.color;
                col.a = Mathf.Lerp(col.a, 0f, progress);
                r.material.color = col;
            }

            if (_footprintProgress[fp] >= footprintCleanTime)
            {
                _footprintProgress.Remove(fp);
                FootprintTracker.RemoveFootprint(fp);
            }
        }

        if (!anyInRange)
            _footprintProgress.Clear();
    }

    // ── Core brush ─────────────────────────────────────────────────────────

    void ApplyBrush(Vector2 uv, float strength, bool spread)
    {
        Debug.Log($"[Mop] ApplyBrush called | cleanedDist: {_cleanedDistance} / {cleanDistanceBeforeDirty}");

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
                Debug.LogWarning("[MopCleaner] ⚠ Mop is dirty! Select bucket (2) and walk to it.");
                UpdateStatusUI();
            }
        }
    }

    public void TryDipMop()
    {
        if (!_mopIsDirty) return;
        DipMop();
    }

    void DipMop()
    {
        _mopIsDirty = false;
        _cleanedDistance = 0f;
        _lastUV = Vector2.negativeInfinity;
        _footprintProgress.Clear();
        Debug.Log("[MopCleaner] ✅ Mop dipped — ready to clean again!");
        UpdateStatusUI();
    }

    void UpdateStatusUI()
    {
        if (statusText == null) return;

        if (_mopIsDirty)
        {
            statusText.text = "⚠ Mop dirty! Press 2 to select bucket, then walk to it.";
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
        _footprintProgress.Clear();
        UpdateStatusUI();
    }
}