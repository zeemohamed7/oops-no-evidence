using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MopCleaner : MonoBehaviour
{
    [Header("Inventory")]
    public ToolInventory inventory;

    [Header("Mop Range")]
    [Tooltip("How close the player must be to a blood pool to mop it (world units).")]
    public float mopRange = 2f;

    [Header("Dirty Mop Settings")]
    public float dipDistance = 1.5f;
    public float cleanDistanceBeforeDirty = 10f;
    public float spreadStrength = 0.5f;

    [Header("Brush")]
    [Tooltip("Fixed world-space radius for both cleaning and dirty spreading — same size on any pool.")]
    public float mopWorldRadius = 0.6f;

    [Header("Footprint Cleaning")]
    public float footprintCleanRadius = 0.4f;
    public float footprintCleanTime = 0.6f;

    [Header("Dirty Mop Trail")]
    [Tooltip("Blood splatter texture for dirty mop marks (assign whiteSplatter or any splat texture).")]
    public Texture2D dirtyMopMarkTexture;
    [Tooltip("Color of the dirty mop smear.")]
    public Color dirtyMopMarkColor = new Color(0.667f, 0f, 0f, 0.85f);
    [Tooltip("Size of each smear decal in world units.")]
    public float dirtyMarkSize = 1.5f;
    [Tooltip("Hard cap to prevent infinite decals. Raise if marks disappear too early.")]
    public int maxDirtyMarks = 300;

    [Header("UI Feedback")]
    public Text statusText;

    [Header("Dirty Indicator")]
    [Tooltip("A world-space GameObject (e.g. Canvas with image) positioned above the player's head. " +
             "Shown when mop is dirty, hidden when clean.")]
    public GameObject dirtyMopIndicator;

    //malak
    [Header("Mop Sound")]
    public AudioSource moppingSoundSource;

    // ── Private ────────────────────────────────────────────────────────────
    bool _painting;
    float _cleanedDistance = 0f;
    Vector2 _lastUV = -Vector2.one;
    bool _mopIsDirty = false;
    BloodPool[] _bloodPools;
    InputAction _interactAction;
    ToolInventory[] _otherInventories;
    private PlayerAnimationDriver animationDriver;
    System.Collections.Generic.Dictionary<GameObject, float> _footprintProgress
        = new System.Collections.Generic.Dictionary<GameObject, float>();

    System.Collections.Generic.List<GameObject> _dirtyMarks
        = new System.Collections.Generic.List<GameObject>();
    float _decalStampTimer = 0f;
    const float DecalStampInterval = 0.25f;

    void Start()
    {
        if (!inventory) inventory = GetComponent<ToolInventory>();
        if (!inventory) { Debug.LogError("[MopCleaner] ToolInventory not found!"); enabled = false; return; }

        _bloodPools = FindObjectsByType<BloodPool>(FindObjectsSortMode.None);
        if (_bloodPools.Length == 0)
            Debug.LogWarning("[MopCleaner] No BloodPool found in scene.");

        var playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            _interactAction = playerInput.actions["Interact"];
        else
            Debug.LogWarning("[MopCleaner] No PlayerInput found — hold-to-mop won't work.");

        animationDriver = GetComponent<PlayerAnimationDriver>();

        //malak
        if (moppingSoundSource != null)
        {
            moppingSoundSource.loop = true;
            moppingSoundSource.playOnAwake = false;
        }

        UpdateStatusUI();
    }

    void Update()
    {
        // Lazy-find runs every frame until other players are found
        if (_otherInventories == null || _otherInventories.Length == 0)
        {
            var all = FindObjectsByType<ToolInventory>(FindObjectsSortMode.None);
            _otherInventories = System.Array.FindAll(all, t => t != inventory);
        }

        // Auto-dip — checked before the early return so it works even if mop is put away
        if (_mopIsDirty)
        {
            foreach (ToolInventory other in _otherInventories)
            {
                if (other == null || !other.IsBucketSelected()) continue;
                float dist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(other.transform.position.x, other.transform.position.z));
                if (dist <= dipDistance) { DipMop(); return; }
            }
        }

        if (!inventory.IsMopSelected()) { 
            _painting = false;
            StopMopSound(); //malak
            return; 
        }

        bool waspainting = _painting;
        _painting = _interactAction != null && _interactAction.IsPressed();

        if (_painting && !waspainting)
        {
            animationDriver?.PlayMop();
            PlayMopSound();//malak
        }
        if (!_painting)
        {
            if (waspainting) { _lastUV = -Vector2.one; _footprintProgress.Clear(); }
            StopMopSound();//malak
            return;
        }

        // Raycast straight down to find BloodPool objects and dirty marks underfoot
        Ray downRay = new Ray(transform.position + Vector3.up * 10f, Vector3.down);
        RaycastHit[] allHits = Physics.RaycastAll(downRay, 30f, ~0, QueryTriggerInteraction.Collide);

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

            if (!_mopIsDirty)
                pool.EraseAtUV(uv, mopWorldRadius);
        }

        // Dirty mop always spawns decals — same look everywhere on the map
        if (_mopIsDirty)
        {
            _decalStampTimer -= Time.deltaTime;
            if (_decalStampTimer <= 0f)
            {
                SpawnDirtyMark(transform.position);
                _decalStampTimer = DecalStampInterval * Random.Range(0.7f, 1.4f);
            }
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
                Debug.LogWarning("[MopCleaner] Mop is dirty! Find the player holding the bucket.");
                UpdateStatusUI();
            }
        }

        // Clean regular footprints (proximity-based)
        if (!_mopIsDirty) CleanFootprintsNear(transform.position);

        // Clean dirty mop splatters (raycast-based, same mechanic as blood pools)
        if (!_mopIsDirty)
        {
            foreach (RaycastHit h in allHits)
            {
                GameObject hit = h.collider.gameObject;
                if (!_dirtyMarks.Contains(hit)) continue;
                _dirtyMarks.Remove(hit);
                StartCoroutine(FadeAndDestroy(hit));
            }
        }
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

    // ── Dirty mark decals ──────────────────────────────────────────────────

    void SpawnDirtyMark(Vector3 worldPos)
    {
// Find the floor Y just below the player (short range avoids hitting floors above in multi-story levels)
        float floorY = worldPos.y;
        if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 3f))
            floorY = hit.point.y;
        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mark.transform.position   = new Vector3(worldPos.x, floorY + 0.02f, worldPos.z);
        mark.transform.rotation   = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        float randomSize = dirtyMarkSize * Random.Range(0.6f, 1.4f);
        mark.transform.localScale = new Vector3(randomSize, randomSize * Random.Range(0.6f, 1f), 1f);
        mark.name = "DirtyMopMark";

        // Keep collider as trigger so the mop raycast can detect it
        var col = mark.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Build a simple transparent material — no foot shape, just a blood blob
        mark.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = dirtyMopMarkTexture,
            color = dirtyMopMarkColor
        };

        _dirtyMarks.Add(mark);

        // Remove and destroy oldest mark when limit reached
        while (_dirtyMarks.Count > maxDirtyMarks)
        {
            GameObject old = _dirtyMarks[0];
            _dirtyMarks.RemoveAt(0);
            if (old != null) Destroy(old);
        }
    }

    void ClearDirtyMarks()
    {
        foreach (var mark in _dirtyMarks)
        {
            if (mark != null) Destroy(mark);
        }
        _dirtyMarks.Clear();
    }

    System.Collections.IEnumerator FadeAndDestroy(GameObject mark)
    {
        Renderer r = mark.GetComponent<Renderer>();
        if (r == null) { Destroy(mark); yield break; }

        float elapsed = 0f;
        float duration = 0.3f;
        Color startColor = r.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            r.material.color = c;
            yield return null;
        }

        Destroy(mark);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void TryDipMop()
    {
        if (!_mopIsDirty) return;
        DipMop();
        animationDriver?.PlayDipMop();
    }

    void DipMop()
    {
        _mopIsDirty = false;
        _cleanedDistance = 0f;
        _lastUV = -Vector2.one;
        _footprintProgress.Clear();
        Debug.Log("[MopCleaner] Mop dipped — ready to clean again!");
        animationDriver?.PlayDipMop();
        UpdateStatusUI();
    }

    void UpdateStatusUI()
    {
        if (dirtyMopIndicator != null)
            dirtyMopIndicator.SetActive(_mopIsDirty);

        if (statusText == null) return;
        if (_mopIsDirty)
            statusText.text = "Mop dirty! Find the player holding the bucket!";
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
        ClearDirtyMarks();
        UpdateStatusUI();
    }

    //malak
    void PlayMopSound()
    {
        if (moppingSoundSource != null && !moppingSoundSource.isPlaying)
        {
            moppingSoundSource.Play();
        }
    }

    void StopMopSound()
    {
        if (moppingSoundSource != null && moppingSoundSource.isPlaying)
        {
            moppingSoundSource.Stop();
        }
    }
}