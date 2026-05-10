using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MopCleaner : MonoBehaviour
{
    [Header("Inventory")]
    public ToolInventory inventory;

    [Header("Mop Range")]
    [Tooltip("How close the player must be to a blood pool to mop it (world units).")]
    public float mopRange = 1.5f;

    [Header("Dirty Mop Settings")]
    public GameObject bucketObject;
    public float dipDistance = 1.5f;
    public float cleanDistanceBeforeDirty = 5.0f;
    [Range(0.002f, 0.15f)] public float spreadStrength = 0.03f;

    [Header("Footprint Cleaning")]
    public float footprintCleanRadius = 0.4f;
    public float footprintCleanTime = 0.6f;

    [Header("UI Feedback")]
    public Text statusText;

    // ── Private ────────────────────────────────────────────────────────────
    bool _painting;
    float _cleanedDistance = 0f;
    Vector2 _lastUV = -Vector2.one;
    bool _mopIsDirty = false;
    BloodPool[] _bloodPools;
    InputAction _interactAction;

    System.Collections.Generic.Dictionary<GameObject, float> _footprintProgress
        = new System.Collections.Generic.Dictionary<GameObject, float>();

    void Start()
    {
        if (!inventory) inventory = GetComponent<ToolInventory>();
        if (!inventory) { Debug.LogError("[MopCleaner] ToolInventory not found!"); enabled = false; return; }

        if (!bucketObject)
            Debug.LogWarning("[MopCleaner] bucketObject not assigned — auto-dip won't work!");

        _bloodPools = FindObjectsByType<BloodPool>(FindObjectsSortMode.None);
        if (_bloodPools.Length == 0)
            Debug.LogWarning("[MopCleaner] No BloodPool found in scene.");

        var playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            _interactAction = playerInput.actions["Interact"];
        else
            Debug.LogWarning("[MopCleaner] No PlayerInput found — hold-to-mop won't work.");

        UpdateStatusUI();
    }

    void Update()
    {
        if (!inventory.IsMopSelected()) { _painting = false; return; }

        // Auto-dip when near bucket
        if (_mopIsDirty
            && bucketObject != null
            && bucketObject.activeInHierarchy
            && Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(bucketObject.transform.position.x, bucketObject.transform.position.z)) <= dipDistance)
        {
            DipMop();
            return;
        }

        bool waspainting = _painting;
        _painting = _interactAction != null && _interactAction.IsPressed();
        if (!_painting)
        {
            if (waspainting) { _lastUV = -Vector2.one; _footprintProgress.Clear(); }
            return;
        }

        // Raycast straight down to find BloodPool objects underfoot
        Ray downRay = new Ray(transform.position + Vector3.up * 10f, Vector3.down);
        RaycastHit[] allHits = Physics.RaycastAll(downRay, 30f);

        bool didClean = false;
        Vector2 cleanUV = -Vector2.one;

        foreach (RaycastHit h in allHits)
        {
            BloodPool pool = h.collider.GetComponent<BloodPool>();
            if (pool == null) continue;

            // MeshCollider gives accurate per-pixel UV; Box/other colliders return (0,0)
            Vector2 uv = (h.collider is MeshCollider)
                ? h.textureCoord
                : pool.WorldToUV(transform.position);

            if (!pool.UVInRange(uv)) continue;

            didClean = true;
            cleanUV = uv;

            if (_mopIsDirty)
                pool.SpreadAtUV(uv, spreadStrength);
            else
                pool.EraseAtUV(uv);
        }

        // Track UV distance for dirty mop (same scale as cleanDistanceBeforeDirty)
        if (didClean && !_mopIsDirty)
        {
            if (_lastUV.x >= 0f)
                _cleanedDistance += Vector2.Distance(cleanUV, _lastUV);
            _lastUV = cleanUV;
            UpdateStatusUI();

            if (_cleanedDistance >= cleanDistanceBeforeDirty)
            {
                _mopIsDirty = true;
                Debug.LogWarning("[MopCleaner] Mop is dirty! Walk to the bucket.");
                UpdateStatusUI();
            }
        }

        // Clean footprints
        if (!_mopIsDirty) CleanFootprintsNear(transform.position);
    }

    // ── Footprint cleaning ─────────────────────────────────────────────────

    void CleanFootprintsNear(Vector3 worldPoint)
    {
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

            if (dist > footprintCleanRadius) { _footprintProgress.Remove(fp); continue; }

            anyInRange = true;

            if (footprintCleanTime <= 0f) { FootprintTracker.RemoveFootprint(fp); continue; }

            if (!_footprintProgress.ContainsKey(fp)) _footprintProgress[fp] = 0f;
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

        if (!anyInRange) _footprintProgress.Clear();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void TryDipMop()
    {
        if (!_mopIsDirty) return;
        DipMop();
    }

    void DipMop()
    {
        _mopIsDirty = false;
        _cleanedDistance = 0f;
        _lastUV = -Vector2.one;
        _footprintProgress.Clear();
        Debug.Log("[MopCleaner] Mop dipped — ready to clean again!");
        UpdateStatusUI();
    }

    void UpdateStatusUI()
    {
        if (statusText == null) return;
        if (_mopIsDirty)
            statusText.text = "Mop dirty! Walk to the bucket.";
        else
        {
            float pct = Mathf.RoundToInt(Mathf.Clamp01(_cleanedDistance / cleanDistanceBeforeDirty) * 100f);
            statusText.text = $"Mop clean — {pct}% used";
        }
    }

    public void ResetBlood()
    {
        foreach (BloodPool pool in _bloodPools)
            if (pool != null) pool.ResetBlood();

        _cleanedDistance = 0f;
        _mopIsDirty = false;
        _lastUV = -Vector2.one;
        _footprintProgress.Clear();
        UpdateStatusUI();
    }
}
