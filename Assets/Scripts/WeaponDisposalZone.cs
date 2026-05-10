using System.Collections;
using UnityEngine;

public class WeaponDisposalZone : MonoBehaviour
{
    [Header("Sparkle")]
    public ParticleSystem sparkles;   // leave empty to auto-create gold burst
    public Light sparkleLight;        // optional Point Light child for flash

    [Header("Disposal")]
    [Tooltip("Seconds the weapon must stay inside before being disposed. Prevents destruction on wall bounce.")]
    public float dwellTime = 0.4f;

    private bool completed;
    private Coroutine _pendingDispose;

    void Start()
    {
        if (sparkles == null)
            sparkles = CreateSparkleSystem();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed || !other.CompareTag("Weapon")) return;
        _pendingDispose = StartCoroutine(DisposeAfterDwell(other));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Weapon") || _pendingDispose == null) return;
        StopCoroutine(_pendingDispose);
        _pendingDispose = null;
    }

    private IEnumerator DisposeAfterDwell(Collider weapon)
    {
        yield return new WaitForSeconds(dwellTime);

        if (weapon == null || completed) yield break;

        completed = true;
        _pendingDispose = null;
        GameEvents.OnTaskCompleted?.Invoke("dispose_weapon");
        Destroy(weapon.gameObject);
        PlaySparkle();
        Debug.Log("Weapon disposed");
    }

    void PlaySparkle()
    {
        if (sparkles != null)
        {
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparkles.Play();
        }
        if (sparkleLight != null)
            StartCoroutine(FlashLight());
    }

    IEnumerator FlashLight()
    {
        sparkleLight.enabled = true;
        float duration = 0.4f, t = 0f;
        float startIntensity = sparkleLight.intensity;
        while (t < duration)
        {
            t += Time.deltaTime;
            sparkleLight.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            yield return null;
        }
        sparkleLight.enabled = false;
        sparkleLight.intensity = startIntensity;
    }

    ParticleSystem CreateSparkleSystem()
    {
        GameObject go = new GameObject("SparkleEffect");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 0.5f;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.startLifetime = 0.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.2f),  // gold
            new Color(1f, 1f, 1f)        // white
        );
        main.gravityModifier = 0.3f;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        return ps;
    }
}