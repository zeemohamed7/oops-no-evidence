using UnityEngine;

/// <summary>
/// BloodPool — attach this to your blood Plane GameObject.
///
/// Responsibilities:
///   1. Owns and initialises the RenderTexture (bloodRT).
///   2. Wires bloodRT into the FloorBloodShader _MaskTex slot.
///   3. Exposes EraseAt(worldPos) so MopCleaner can clean it.
///   4. Exposes SampleBloodAt(worldPos) so FootprintTracker can
///      read how much blood is at a given world position.
///   5. Exposes StampFootprint(worldPos) to paint a small dark
///      circle onto the RT (footprint mark on the pool itself).
///
/// All world-to-UV conversion lives here — nothing else needs to
/// know about the RT dimensions or the plane's world bounds.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class BloodPool : MonoBehaviour
{
    [Header("Texture")]
    [Tooltip("Drag your whiteSplatter Texture2D here — defines the initial blood shape.")]
    public Texture2D initialBloodMask;

    [Tooltip("RenderTexture asset from your Project (ARGB32, any square power-of-2 size).")]
    public RenderTexture bloodRT;

    [Header("Shader Property")]
    [Tooltip("Reference name of the mask texture property inside FloorBloodShader. " +
             "Open the ShaderGraph → Blackboard and check the Texture2D property name.")]
    public string maskTexProperty = "_MaskTex";

    [Header("Cleaning")]
    [Tooltip("Radius of the mop brush in UV space. 0.05 ≈ one floor tile.")]
    [Range(0.005f, 0.25f)] public float brushRadius = 0.05f;

    [Tooltip("Blood removed per frame while mopping. Keep low for gradual cleaning.")]
    [Range(0.002f, 0.15f)] public float brushStrength = 0.04f;

    [Header("Footprint stamp")]
    [Tooltip("Radius of each footprint mark stamped onto the pool (UV space).")]
    [Range(0.002f, 0.05f)] public float footprintRadius = 0.012f;

    [Tooltip("How strongly the footprint darkens the pool RT.")]
    [Range(0.1f, 1f)] public float footprintStrength = 0.6f;

    // ── Private ────────────────────────────────────────────────────────────
    Renderer _renderer;
    Material _floorMat;      // instanced floor material
    Material _eraseMat;      // MopClean shader instance
    Material _stampMat;      // MopClean shader — used in reverse for footprint stamps
    RenderTexture _tempRT;

    Shader _blitShader;

    static readonly int ID_HitUV    = Shader.PropertyToID("_HitUV");
    static readonly int ID_Radius   = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");
    static readonly int ID_MaskTex  = Shader.PropertyToID("_MaskTex");
    static readonly int ID_Spread   = Shader.PropertyToID("_Spread");

    // ── Bounds helpers ─────────────────────────────────────────────────────
    // We derive world bounds from this GameObject's Renderer bounds at Start.
    Bounds _worldBounds;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _worldBounds = _renderer.bounds;

        // Instance the floor material so we don't mutate the shared asset
        _floorMat = _renderer.material;   // .material already instances it

        // Load the blit shader
        _blitShader = Shader.Find("Custom/MopClean");
        if (_blitShader == null)
        {
            Debug.LogError("[BloodPool] Cannot find shader 'Custom/MopClean'. " +
                           "Make sure MopClean.shader is imported and named correctly.");
            enabled = false;
            return;
        }

        _eraseMat = new Material(_blitShader);
        _stampMat = new Material(_blitShader);

        // Ping-pong buffer
        _tempRT = new RenderTexture(bloodRT.descriptor);
        _tempRT.name = "BloodPool_Temp";
        _tempRT.Create();

        // Seed the RT from the splat texture
        ResetBlood();

        // Wire bloodRT into the floor shader
        _floorMat.SetTexture(maskTexProperty, bloodRT);
        Debug.Log($"[BloodPool] '{name}' ready. RT wired to [{maskTexProperty}].");
    }

    void OnDestroy()
    {
        if (_eraseMat) Destroy(_eraseMat);
        if (_stampMat) Destroy(_stampMat);
        if (_tempRT) { _tempRT.Release(); Destroy(_tempRT); }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Erase a brush circle at the given world position.
    /// Called every frame by MopCleaner while the player is mopping nearby.
    /// </summary>
    public void EraseAt(Vector3 worldPos)
    {
        Vector2 uv = WorldToUV(worldPos);
        if (!InRange(uv)) return;

        _eraseMat.SetVector(ID_HitUV, new Vector4(uv.x, uv.y, 0, 0));
        _eraseMat.SetFloat(ID_Radius, brushRadius);
        _eraseMat.SetFloat(ID_Strength, brushStrength);

        Blit(_eraseMat);
    }

    /// <summary>
    /// Spread (paint) blood back at the given world position — used when mop is dirty.
    /// </summary>
    public void SpreadAt(Vector3 worldPos, float strength)
    {
        Vector2 uv = WorldToUV(worldPos);
        if (!InRange(uv)) return;

        _eraseMat.SetVector(ID_HitUV, new Vector4(uv.x, uv.y, 0, 0));
        _eraseMat.SetFloat(ID_Radius, brushRadius);
        _eraseMat.SetFloat(ID_Strength, strength);
        _eraseMat.SetFloat(ID_Spread, 1f);
        Blit(_eraseMat);
        _eraseMat.SetFloat(ID_Spread, 0f);
    }

    /// <summary>
    /// Returns the blood coverage (0=clean, 1=full blood) at a world position.
    /// Used by FootprintTracker to decide whether to start leaving prints.
    /// Reads a single pixel from the RT — cheap on GPU.
    /// </summary>
    public float SampleBloodAt(Vector3 worldPos)
    {
        Vector2 uv = WorldToUV(worldPos);
        if (!InRange(uv)) return 0f;

        // Read one pixel from the RT at the UV position
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = bloodRT;

        int px = Mathf.Clamp((int)(uv.x * bloodRT.width), 0, bloodRT.width - 1);
        int py = Mathf.Clamp((int)(uv.y * bloodRT.height), 0, bloodRT.height - 1);

        Texture2D sample = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        sample.ReadPixels(new Rect(px, py, 1, 1), 0, 0);
        sample.Apply();
        float val = sample.GetPixel(0, 0).r;
        Destroy(sample);

        RenderTexture.active = prev;
        return val;
    }

    /// <summary>
    /// Stamp a small dark footprint mark onto the pool at the given world position.
    /// This darkens (partially erases) the RT so the footprint shape is visible.
    /// </summary>
    public void StampFootprintAt(Vector3 worldPos)
    {
        Vector2 uv = WorldToUV(worldPos);
        if (!InRange(uv)) return;

        _stampMat.SetVector(ID_HitUV, new Vector4(uv.x, uv.y, 0, 0));
        _stampMat.SetFloat(ID_Radius, footprintRadius);
        _stampMat.SetFloat(ID_Strength, footprintStrength);

        Blit(_stampMat);
    }

    /// <summary>Restore the blood pool to its initial splat shape.</summary>
    public void ResetBlood()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = bloodRT;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;

        if (initialBloodMask != null)
            Graphics.Blit(initialBloodMask, bloodRT);
        else
            Debug.LogWarning($"[BloodPool] '{name}' has no initialBloodMask — pool starts clean.");
    }

    /// <summary>
    /// Returns true if the given world XZ position overlaps this blood pool.
    /// Used by MopCleaner and FootprintTracker for proximity checks.
    /// </summary>
    public bool IsNearby(Vector3 worldPos, float radius)
    {
        // Expand bounds by radius and test XZ only
        float dx = Mathf.Max(0, Mathf.Abs(worldPos.x - _worldBounds.center.x) - _worldBounds.extents.x);
        float dz = Mathf.Max(0, Mathf.Abs(worldPos.z - _worldBounds.center.z) - _worldBounds.extents.z);
        return (dx * dx + dz * dz) <= (radius * radius);
    }

    // ── Internal helpers ───────────────────────────────────────────────────

    void Blit(Material mat)
    {
        Graphics.Blit(bloodRT, _tempRT);
        Graphics.Blit(_tempRT, bloodRT, mat);
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = Mathf.InverseLerp(_worldBounds.min.x, _worldBounds.max.x, worldPos.x);
        float v = Mathf.InverseLerp(_worldBounds.min.z, _worldBounds.max.z, worldPos.z);
        return new Vector2(u, v);
    }

    static bool InRange(Vector2 uv) =>
        uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
}