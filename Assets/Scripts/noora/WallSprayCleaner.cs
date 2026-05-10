using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the Player. Spray (slot 4) + Interact (Y / E) cleans wall blood
/// in two presses: first fades the blood 50%, second clears it completely.
///
/// Works with any wall object whose material uses "_MaskTex" as its blood
/// render texture (same shader as the floor blood system).
/// Tag your wall-blood GameObjects "BloodWall", OR set bloodMaterialName to
/// a substring of the material's name so detection works without a tag.
/// </summary>
public class WallSprayCleaner : MonoBehaviour
{ 
    [Header("References")]
    public ToolInventory inventory;

    [Header("Spray Settings")]
    public float sprayRange = 4f;
    public LayerMask wallMask = ~0;

    [Header("Blood Detection")]
    [Tooltip("Tag on wall-blood GameObjects.")]
    public string bloodWallTag = "BloodWall";

    [Tooltip("Part of the material name used as a fallback if the tag is not found.")]
    public string bloodMaterialName = "WallWithBlood";

    [Tooltip("Shader property name for the blood mask render texture.")]
    public string maskTexProperty = "_MaskTex";

    readonly Dictionary<Renderer, int> _sprayCount = new Dictionary<Renderer, int>();

    Material _fadeMat;

    static readonly int ID_HitUV   = Shader.PropertyToID("_HitUV");
    static readonly int ID_Radius  = Shader.PropertyToID("_Radius");
    static readonly int ID_Strength = Shader.PropertyToID("_Strength");
    static readonly int ID_Spread  = Shader.PropertyToID("_Spread");

    void Awake()
    {
        Shader blitShader = Shader.Find("Custom/MopClean");
        if (blitShader != null)
            _fadeMat = new Material(blitShader);
        else
            Debug.LogWarning("[WallSpray] MopClean shader not found — RT fading won't work.");
    }

    void OnDestroy()
    {
        if (_fadeMat) Destroy(_fadeMat);
    }

    // Called by PlayerInput (Send Messages behavior) when Interact action fires.
    public void OnInteract()
    {
        if (inventory == null || !inventory.IsSpraySelected()) return;
        TrySpray();
    }

    void TrySpray()
    {
        Ray ray = BuildRay();
        Debug.DrawRay(ray.origin, ray.direction * sprayRange, Color.cyan, 2f);

        if (!Physics.Raycast(ray, out RaycastHit hit, sprayRange, wallMask))
        {
            Debug.LogWarning("[WallSpray] Raycast missed everything. Face the wall and move closer.");
            return;
        }

        Debug.LogWarning($"[WallSpray] Raycast hit '{hit.collider.gameObject.name}' (tag='{hit.collider.tag}')");

        Renderer rend = FindBloodRenderer(hit);
        if (rend == null)
        {
            Debug.LogWarning($"[WallSpray] No blood renderer. Tag it '{bloodWallTag}' or ensure material name contains '{bloodMaterialName}'.");
            return;
        }

        ApplySpray(rend);
    }

    void ApplySpray(Renderer rend)
    {
        if (!_sprayCount.ContainsKey(rend)) _sprayCount[rend] = 0;
        _sprayCount[rend]++;

        // Primary approach: modify the blood render texture directly.
        RenderTexture bloodRT = rend.sharedMaterial.GetTexture(maskTexProperty) as RenderTexture;
        if (bloodRT != null)
        {
            if (_sprayCount[rend] == 1)
            {
                FadeRenderTexture(bloodRT, 0.5f);
                Debug.LogWarning("[WallSpray] First spray — blood faded 50%.");
            }
            else
            {
                ClearRenderTexture(bloodRT);
                _sprayCount.Remove(rend);
                Debug.LogWarning("[WallSpray] Second spray — blood cleared!");
            }
            return;
        }

        // Fallback: material color alpha (for non-RT materials).
        Debug.LogWarning("[WallSpray] No _MaskTex RT found — falling back to alpha.");
        Material mat = rend.material;
        if (_sprayCount[rend] == 1)
            SetAlpha(mat, 0.5f);
        else
        {
            SetAlpha(mat, 0f);
            _sprayCount.Remove(rend);
        }
    }

    // Subtract fadeAmount from every pixel in the RT using the MopClean shader
    // with an enormous radius so it covers the whole texture uniformly.
    void FadeRenderTexture(RenderTexture rt, float fadeAmount)
    {
        if (_fadeMat == null) return;

        _fadeMat.SetVector(ID_HitUV,    new Vector4(0.5f, 0.5f, 0, 0));
        _fadeMat.SetFloat(ID_Radius,    100f);   // covers entire UV space
        _fadeMat.SetFloat(ID_Strength,  fadeAmount);
        _fadeMat.SetFloat(ID_Spread,    0f);     // clean mode

        RenderTexture temp = RenderTexture.GetTemporary(rt.descriptor);
        Graphics.Blit(rt, temp);
        Graphics.Blit(temp, rt, _fadeMat);
        RenderTexture.ReleaseTemporary(temp);
    }

    static void ClearRenderTexture(RenderTexture rt)
    {
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;
    }

    // Shoot horizontally from the player in their facing direction.
    // (Top-down cameras shoot downward — they miss vertical walls.)
    Ray BuildRay()
    {
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        Vector3 dir    = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        return new Ray(origin, dir);
    }

    Renderer FindBloodRenderer(RaycastHit hit)
    {
        if (!string.IsNullOrEmpty(bloodWallTag) && hit.collider.CompareTag(bloodWallTag))
        {
            Renderer r = hit.collider.GetComponent<Renderer>()
                      ?? hit.collider.GetComponentInChildren<Renderer>();
            if (r != null) return r;
        }

        Renderer wallRend = hit.collider.GetComponent<Renderer>();
        if (wallRend != null && !string.IsNullOrEmpty(bloodMaterialName))
        {
            foreach (Material mat in wallRend.sharedMaterials)
                if (mat != null && mat.name.Contains(bloodMaterialName))
                    return wallRend;
        }

        return null;
    }

    static void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor"); c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color; c.a = alpha;
            mat.color = c;
        }
    }
}
