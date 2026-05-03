using UnityEngine;

public class EngineSound : MonoBehaviour
{
    private AudioSource _audioSource;
    private Vector3 _lastPosition;
    
    [Header("Pitch Settings")]
    public float minPitch = 0.7f;  // Pitch when idle
    public float maxPitch = 1.5f;  // Pitch at top speed
    public float pitchSensitivity = 0.5f; // How fast it reacts to speed

    [Header("Volume Settings")]
    public float minVolume = 0.4f;
    public float maxVolume = 0.8f;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Calculate how fast the truck is moving this frame
        float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
        float currentSpeed = distanceMoved / Time.deltaTime;


        float speedRatio = Mathf.Clamp01(currentSpeed / 10f);

        // 3. Apply the Pitch Shift
        // Lerp smoothly moves between the min and max pitch
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
        _audioSource.pitch = Mathf.MoveTowards(_audioSource.pitch, targetPitch, pitchSensitivity * Time.deltaTime);

        // 4. Slightly adjust volume so it sounds "quieter" at idle
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
        _audioSource.volume = targetVolume;

        // 5. Save position for the next frame
        _lastPosition = transform.position;
    }
}