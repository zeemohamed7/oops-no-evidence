using UnityEngine;

/// <summary>
/// FootprintTracker — attach to Player only.
///
/// When the player walks through blood they pick it up on their shoes.
/// For the next few steps they leave red footprint decals that fade out.
/// After [maxPrintSteps] steps the shoes are clean and no more prints appear.
/// </summary>
public class FootprintTracker : MonoBehaviour
{
    [Header("Footprint Decal")]
    [Tooltip("A Quad prefab lying flat (rotated X=90). " +
             "Must have NO Collider, or a Collider set to Is Trigger. " +
             "Assign a material using Custom/Footprint shader.")]
    public GameObject footprintPrefab;

    [Tooltip("How long each footprint stays visible before being destroyed (seconds).")]
    public float footprintLifetime = 15f;

    [Tooltip("World-space size of each footprint decal.")]
    public float footprintSize = 0.25f;

    [Header("Steps")]
    [Tooltip("Distance walked between each footprint stamp (world units).")]
    [Range(0.2f, 1f)] public float stepDistance = 0.45f;

    [Tooltip("How many footprint steps the player leaves after walking through blood. " +
             "Prints fade out across these steps.")]
    [Range(1, 20)] public int maxPrintSteps = 8;

    [Header("Blood Detection")]
    [Tooltip("How often to check if the player is standing on blood (seconds).")]
    [Range(0.05f, 0.3f)] public float sampleInterval = 0.1f;

    [Tooltip("Minimum blood value at player feet to pick up blood on shoes (0–1).")]
    [Range(0.05f, 0.5f)] public float bloodPickupThreshold = 0.15f;

    // ── Private ────────────────────────────────────────────────────────────
    int _stepsRemaining;      // how many more prints to leave
    bool _leftFoot = true;     // alternate feet
    float _distAccum;           // distance walked since last step
    Vector3 _lastPos;
    float _sampleTimer;

    BloodPool[] _allPools;
    float _poolRefreshTimer;
    const float POOL_REFRESH = 0.3f;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        _lastPos = transform.position;
        RefreshPools();
    }

    void Update()
    {
        // Refresh pool list occasionally
        _poolRefreshTimer -= Time.deltaTime;
        if (_poolRefreshTimer <= 0f) RefreshPools();

        // Sample blood under feet
        _sampleTimer -= Time.deltaTime;
        if (_sampleTimer <= 0f)
        {
            _sampleTimer = sampleInterval;
            CheckBloodUnderFeet();
        }

        // Track distance walked
        float moved = Vector3.Distance(transform.position, _lastPos);
        _lastPos = transform.position;

        if (moved < 0.001f) return;   // standing still — no footprint
        _distAccum += moved;

        // Time for a new footprint?
        if (_distAccum >= stepDistance && _stepsRemaining > 0)
        {
            _distAccum = 0f;
            SpawnFootprint();
            _stepsRemaining--;
        }
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
                // Player stepped in blood — reset step counter
                _stepsRemaining = maxPrintSteps;
                break;
            }
        }
    }

    // ── Footprint spawning ─────────────────────────────────────────────────

    void SpawnFootprint()
    {
        if (footprintPrefab == null) return;

        // Alternate left / right foot with a small lateral offset
        float side = _leftFoot ? -0.12f : 0.12f;
        _leftFoot = !_leftFoot;
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 spawnPos = transform.position + right * side;
        spawnPos.y = GetFloorY(spawnPos) + 0.01f;   // tiny lift to avoid z-fight

        // Rotate to match player's facing direction, flat on the floor
        Quaternion rot = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

        GameObject fp = Instantiate(footprintPrefab, spawnPos, rot);

        // Scale
        fp.transform.localScale = Vector3.one * footprintSize;

        // ── CRITICAL: remove any collider so the player doesn't get stuck ──
        foreach (Collider c in fp.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;   // disable it — safest option
            // alternatively: c.isTrigger = true;
        }

        // Fade alpha based on how many steps remain (more steps = darker print)
        float alpha = (float)_stepsRemaining / maxPrintSteps;
        Renderer r = fp.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            // Instance the material so we don't mutate the shared asset
            Material mat = r.material;
            Color col = mat.color;
            col.a = alpha;
            mat.color = col;
        }

        Destroy(fp, footprintLifetime);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    float GetFloorY(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f))
            return hit.point.y;
        return transform.position.y;
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
}