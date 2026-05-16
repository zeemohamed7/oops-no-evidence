using System.Collections.Generic;
using UnityEngine;

// Attach to the GameObject that has the Spotlight Light component.
// Fingerprints (FingerprintSurface) fade in when inside the cone.
[RequireComponent(typeof(Light))]
public class FlashlightReveal : MonoBehaviour
{
    [Range(0.5f, 8f)]
    public float fadeSpeed = 3f;

    private Light _spotlight;
    private readonly List<(Transform t, Renderer rend, Material mat)> _fingerprints = new();

    // Awake (not Start) so initialization runs before InventorySystem.Start()
    // disables the flashlight GameObject.
    void Awake()
    {
        _spotlight = GetComponent<Light>();

        foreach (var fp in FindObjectsOfType<FingerprintSurface>())
        {
            var rend = fp.GetComponent<Renderer>();
            rend.enabled = false;
            var mat = rend.material; // per-instance copy
            SetAlpha(mat, 0f);
            _fingerprints.Add((fp.transform, rend, mat));
        }
    }

    void OnDisable()
    {
        foreach (var (_, rend, mat) in _fingerprints)
        {
            SetAlpha(mat, 0f);
            rend.enabled = false;
        }
    }

    void Update()
    {
        float cosHalfAngle = Mathf.Cos(Mathf.Deg2Rad * _spotlight.spotAngle * 0.5f);
        float rangeSq = _spotlight.range * _spotlight.range;

        foreach (var (t, rend, mat) in _fingerprints)
        {
            float target = InCone(t.position, cosHalfAngle, rangeSq) ? 1f : 0f;
            float current = mat.GetColor("_BloodColor").a;

            if (target > 0f && !rend.enabled)
            {
                rend.enabled = true;
                current = 0f;
                SetAlpha(mat, 0f);
            }

            float next = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);
            SetAlpha(mat, next);

            if (next <= 0f)
                rend.enabled = false;
        }
    }

    bool InCone(Vector3 worldPos, float cosHalfAngle, float rangeSq)
    {
        Vector3 toTarget = worldPos - transform.position;
        if (toTarget.sqrMagnitude > rangeSq) return false;
        return Vector3.Dot(transform.forward, toTarget.normalized) >= cosHalfAngle;
    }

    void SetAlpha(Material mat, float alpha)
    {
        Color c = mat.GetColor("_BloodColor");
        c.a = alpha;
        mat.SetColor("_BloodColor", c);
    }
}
