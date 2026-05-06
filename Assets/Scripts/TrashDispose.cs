using System.Collections;
using UnityEngine;

// Attach to the trash GameObject.
// Requires a Collider with Is Trigger = true on the trash.
// Tag the gun GameObject as "Gun".
public class TrashDispose : MonoBehaviour
{
    [Header("Sparkle")]
    public ParticleSystem sparkles;    // assign a child Particle System, or leave empty to auto-create
    public Light sparkleLight;         // optional Point Light child for a flash

    [Header("Task")]
    [Tooltip("Which GameHUD task index to complete when gun is disposed (0, 1, or 2)")]
    public int taskIndex = 1;
    public string gunTag = "Gun";

    void Start()
    {
        if (sparkles == null)
            sparkles = CreateSparkleSystem();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(gunTag)) return;

        GrabbableObject grab = other.GetComponent<GrabbableObject>();
        if (grab == null || !grab.isGrabbed) return;

        // Clean up the joint on the holder
        grab.currentHolder?.GetComponent<Grab>()?.ForceRelease();

        // Destroy the gun
        Destroy(other.gameObject);

        // Effects
        PlaySparkle();

        // Mark task done
        GameHUD.Instance?.CompleteTask(taskIndex);

        Debug.Log("Gun disposed in trash!");
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
        float t = 0f;
        float duration = 0.4f;
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

    // Creates a gold sparkle burst particle system as a child if none is assigned
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
            new Color(1f, 0.9f, 0.2f),   // gold
            new Color(1f, 1f, 1f)         // white
        );
        main.gravityModifier = 0.3f;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        return ps;
    }
}
