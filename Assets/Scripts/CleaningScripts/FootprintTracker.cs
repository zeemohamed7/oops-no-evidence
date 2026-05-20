using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// FootprintTracker — attach to Player only.
///
/// When the player walks through blood they pick it up on their shoes.
/// For the next few steps they leave red footprint decals.
/// Footprints persist until mopped — they never auto-destroy.
/// </summary>
public class FootprintTracker : MonoBehaviour
{
    [Header("Footprint Decal")]
    [Tooltip("A Quad prefab lying flat (rotated X=90). " +
             "Must have NO Collider, or a Collider set to Is Trigger. " +
             "Assign a material using Custom/Footprint shader.")]
    public GameObject footprintPrefab;

    [Tooltip("World-space size of each footprint decal.")]
    public float footprintSize = 0.7f;

    [Header("Steps")]
    [Tooltip("Distance walked between each footprint stamp (world units).")]
    [Range(0.2f, 1f)] public float stepDistance = 0.45f;

    [Tooltip("How many footprint steps the player leaves after walking through blood.")]
    [Range(1, 20)] public int maxPrintSteps = 8;

    [Header("Blood Detection")]
    [Tooltip("How often to check if the player is standing on blood (seconds).")]
    [Range(0.05f, 0.3f)] public float sampleInterval = 0.1f;

    [Tooltip("Minimum blood value at player feet to pick up blood on shoes (0-1).")]
    [Range(0.05f, 0.5f)] public float bloodPickupThreshold = 0.15f;

    [Header("Inventory")]
    public ToolInventory inventory;

    [Header("Floor Detection")]
    [Tooltip("Set this to the layer your floor Plane is on. " +
             "Prevents footprints from snapping to the player collider.")]
    public LayerMask floorLayerMask = ~0;

    [Tooltip("How far above the detected floor surface to place the footprint decal.")]
    public float footprintYOffset = 0.106f;

    // ── Private ────────────────────────────────────────────────────────────
    int _stepsRemaining;
    bool _leftFoot = true;
    float _distAccum;
    Vector3 _lastPos;
    float _sampleTimer;

    BloodPool[] _allPools;
    float _poolRefreshTimer;
    const float POOL_REFRESH = 0.3f;
    InputAction _interactAction;

    // ── Static list — MopCleaner reads this to find footprints ────────────
    public static List<GameObject> ActiveFootprints = new List<GameObject>();

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    // Purge stale footprint references left over from the previous scene
    void OnSceneChanged(Scene prev, Scene next)
    {
        ActiveFootprints.RemoveAll(fp => fp == null);
    }

    void Start()
    {
        if (!inventory) inventory = GetComponent<ToolInventory>() ?? GetComponentInParent<ToolInventory>();

        var pi = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (pi != null) _interactAction = pi.actions.FindAction("Interact");

        _lastPos = transform.position;
        RefreshPools();
    }

    void Update()
    {
        _poolRefreshTimer -= Time.deltaTime;
        if (_poolRefreshTimer <= 0f) RefreshPools();

        _sampleTimer -= Time.deltaTime;
        if (_sampleTimer <= 0f)
        {
            _sampleTimer = sampleInterval;
            CheckBloodUnderFeet();
        }

        float moved = Vector3.Distance(transform.position, _lastPos);
        _lastPos = transform.position;

        if (moved < 0.001f) return;
        _distAccum += moved;

        bool mopping = inventory != null && inventory.IsMopSelected()
                       && _interactAction != null && _interactAction.IsPressed();
        if (_distAccum >= stepDistance && _stepsRemaining > 0 && !mopping)
        {
            _distAccum = 0f;
            SpawnFootprint();
            _stepsRemaining--;
        }
    }

    void OnDestroy()
    {
        // Clean up null entries when player object is destroyed
        ActiveFootprints.RemoveAll(fp => fp == null);
    }

    // ── Blood detection ────────────────────────────────────────────────────

    void CheckBloodUnderFeet()
    {
        foreach (BloodPool pool in _allPools)
        {
            if (pool == null) continue;

            float blood = pool.SampleBloodAt(transform.position);
            if (blood >= bloodPickupThreshold)
            {
                _stepsRemaining = maxPrintSteps;
                break;
            }
        }
    }

    // ── Footprint spawning ─────────────────────────────────────────────────

    void SpawnFootprint()
    {
        if (footprintPrefab == null) return;

        float side = _leftFoot ? -0.12f : 0.12f;
        _leftFoot = !_leftFoot;
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 spawnPos = transform.position + right * side;

        // Snap to floor surface — ray only hits floor layer, never player
        Collider playerCol = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        float feetY = playerCol != null ? playerCol.bounds.min.y : transform.position.y;
        spawnPos.y = feetY + footprintYOffset;
        Quaternion rot = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

        GameObject fp = Instantiate(footprintPrefab, spawnPos, rot);
        fp.transform.localScale = Vector3.one * footprintSize;
        fp.tag = "Footprint";

        // Disable colliders so player never trips on them
        foreach (Collider c in fp.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Fade alpha: more steps remaining = darker/more visible print
        float alpha = (float)_stepsRemaining / maxPrintSteps;
        Renderer r = fp.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Material mat = r.material;
            Color col = mat.color;
            col.a = alpha;
            mat.color = col;
        }

        // ── No Destroy call — footprint stays until player mops it ──
        ActiveFootprints.Add(fp);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    float GetFloorY(Vector3 pos)
    {
        // Cast from high above downward, only hitting floor layer
        Vector3 origin = new Vector3(pos.x, 10f, pos.z); // start well above
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, floorLayerMask))
            return hit.point.y;

        return 0f; // fallback — floor at zero
    }

    void RefreshPools()
    {
        _allPools = Object.FindObjectsByType<BloodPool>(FindObjectsSortMode.None);
        _poolRefreshTimer = POOL_REFRESH;
    }

    /// <summary>Instantly clears shoe blood (e.g. player steps on a clean mat).</summary>
    public void CleanShoes() => _stepsRemaining = 0;

    /// <summary>True if the player currently has bloody shoes.</summary>
    public bool HasBloodyShoes => _stepsRemaining > 0;

    /// <summary>Called by MopCleaner to remove a specific footprint.</summary>
    public static void RemoveFootprint(GameObject fp)
    {
        ActiveFootprints.Remove(fp);
        Destroy(fp);
    }

    /// <summary>Returns how many footprints are currently on the floor.</summary>
    public static int FootprintCount => ActiveFootprints.Count;
}