using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDisposalZone : MonoBehaviour
{
    [Header("Sparkle")]
    public ParticleSystem sparkles;
    public Light sparkleLight;

    [Header("Prompt")]
    public GameObject promptUI;   // optional "Press E to dispose" world UI

    private bool completed;

    void Start()
    {
        if (sparkles == null)
            sparkles = CreateSparkleSystem();

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        if (!other.CompareTag("Weapon")) return;

        completed = true;
        GameEvents.OnTaskCompleted?.Invoke("dispose_weapon");
        PlaySparkle();
        if (promptUI != null) promptUI.SetActive(false);
        Destroy(other.gameObject);
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
            new Color(1f, 0.9f, 0.2f),
            new Color(1f, 1f, 1f)
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
