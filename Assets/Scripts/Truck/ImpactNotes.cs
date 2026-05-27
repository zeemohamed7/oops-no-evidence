using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpactNoise : MonoBehaviour
{
    [Header("Impact Thresholds")]
    [Tooltip("Minimum collision speed (m/s) to register as a noise event. Gentle rests ignored.")]
    public float minImpactVelocity = 1.5f;

    [Tooltip("Collision speed that maps to maxSusSpike. Anything above is capped.")]
    public float maxImpactVelocity = 6f;

    [Header("Suspicion Spike")]
    [Tooltip("Sus added for the lightest qualifying impact.")]
    public float minSusSpike = 8f;

    [Tooltip("Sus added for the hardest qualifying impact.")]
    public float maxSusSpike = 15f;

    [Header("Cooldown")]
    [Tooltip("Seconds before the same object can trigger noise again.")]
    public float cooldown = 1f;

    float _lastImpactTime = -999f;

    void OnCollisionEnter(Collision collision)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (Time.time - _lastImpactTime < cooldown) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactVelocity) return;

        _lastImpactTime = Time.time;

        float t      = Mathf.InverseLerp(minImpactVelocity, maxImpactVelocity, speed);
        float spike  = Mathf.Lerp(minSusSpike, maxSusSpike, t);

        GameEvents.OnSuspicionAdded?.Invoke(spike);
        Debug.Log($"[ImpactNoise] {gameObject.name} hit at {speed:F1} m/s → +{spike:F1} sus");
    }
}