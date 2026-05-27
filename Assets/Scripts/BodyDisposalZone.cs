using System.Collections;
using UnityEngine;

public class BodyDisposalZone : MonoBehaviour
{
    [Header("Sparkle")]
    public ParticleSystem sparkles;
    public Light sparkleLight;

    [Header("Prompt")]
    public GameObject promptUI;

    //malak
    [Header("Disposal Sound")]
    public AudioSource disposalSoundSource;

    [Header("Highlight (while carrying a body)")]
    [Range(1f, 20f)] public float highlightWidth = 10f;

    private bool completed;
    private Outline _outline;

    void Awake()
    {
        _outline = transform.root.GetComponentInChildren<Outline>();
        if (_outline == null)
        {
            MeshRenderer mr = transform.root.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                _outline = mr.gameObject.AddComponent<Outline>();
                _outline.OutlineMode = Outline.Mode.OutlineAll;
                _outline.OutlineWidth = highlightWidth;
            }
        }
        if (_outline != null) _outline.enabled = false;
    }

    void OnEnable()
    {
        GameEvents.OnCarryStart += OnCarryStart;
        GameEvents.OnCarryStop  += OnCarryStop;
    }

    void OnDisable()
    {
        GameEvents.OnCarryStart -= OnCarryStart;
        GameEvents.OnCarryStop  -= OnCarryStop;
    }

    void OnCarryStart(bool isBody)
    {
        if (!isBody || completed || _outline == null) return;
        _outline.OutlineColor = new Color32(157, 0, 255, 255);
        _outline.OutlineWidth = highlightWidth;
        _outline.enabled = true;
    }

    void OnCarryStop()
    {
        if (_outline != null) _outline.enabled = false;
    }

    void Start()
    {
        if (sparkles == null)
            sparkles = CreateSparkleSystem();

        if (promptUI != null)
            promptUI.SetActive(false);

        //malak
        if (disposalSoundSource != null)
        {
            disposalSoundSource.playOnAwake = false;
            disposalSoundSource.loop = false;
        } 
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        GrabbableObject grabbable = other.GetComponentInParent<GrabbableObject>();
        if (grabbable == null || !grabbable.isRagdoll) return;

        completed = true;
        OnCarryStop();
        GameEvents.OnTaskCompleted?.Invoke("dispose_body");
        PlaySparkle();
        PlayDisposalSound(); //malak

        if (promptUI != null)
            promptUI.SetActive(false);

        // PlayerAnimationDriver[] players = FindObjectsByType<PlayerAnimationDriver>(FindObjectsSortMode.None);
        //
        // foreach (PlayerAnimationDriver playerAnim in players)
        // {
        //     playerAnim.SetCarrying(false);
        //     playerAnim.PlayDrop();
        // }
    
        // Logic to DROP body before destroying it
        DeadbodyCarry carryScript = grabbable.GetComponentInParent<DeadbodyCarry>();

        if (carryScript != null)
        {
            // Find every single player script currently tracking carrying mechanics
            Grab[] allActiveGrabbers = FindObjectsByType<Grab>(FindObjectsSortMode.None);
    
            foreach (Grab grabber in allActiveGrabbers)
            {
                // If this specific player is holding the object currently being vaporized
                if (grabber.GetHeldObject() == grabbable.gameObject)
                {
                    // Force them to drop to bring back normal speed 
                    grabber.Drop(); 
                }
            }
        }
        else
        {
            // Fallback fail-safe: Clean out all animations globally if no weight script was attached
            PlayerAnimationDriver[] players = FindObjectsByType<PlayerAnimationDriver>(FindObjectsSortMode.None);
            foreach (PlayerAnimationDriver playerAnim in players)
            {
                playerAnim.SetCarrying(false);
                playerAnim.PlayDrop();
            }
        }
        
        grabbable.transform.SetParent(null);
        Destroy(grabbable.gameObject);

        Debug.Log("Body disposed");
    }

    //malak
    void PlayDisposalSound()
    {
        if (disposalSoundSource != null)
        {
            disposalSoundSource.Play();
        }
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
}git add Assets/Scenes/Levels/Level3.unity Assets/Scripts/BodyDisposalZone.cs
