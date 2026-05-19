using UnityEngine;

public class TruckLevelSelection : MonoBehaviour
{
    [Header("Movement Settings")]
    public float driveSpeed = 500f; 
    public Vector2 offset;

    [Header("References")]
    public VehicleVibration vibrationScript;
    public AudioSource truckAudioSource;

    private RectTransform truckRect;
    private Vector2 targetPos;
    private bool isMoving = false;
    private float originalVolume;

    void Awake()
    {
        truckRect = GetComponent<RectTransform>();
        if (truckAudioSource != null) originalVolume = truckAudioSource.volume;
    }

    void Update()
    {
        if (isMoving)
        {
            // MoveTowards provides that "Normal Vehicle" constant speed
            truckRect.anchoredPosition = Vector2.MoveTowards(
                truckRect.anchoredPosition, 
                targetPos, 
                driveSpeed * Time.deltaTime
            );

            // Check if we arrived (using a small epsilon like 0.1)
            if (Vector2.Distance(truckRect.anchoredPosition, targetPos) < 0.1f)
            {
                OnArrived();
            }
        } // If not moving, play audio
        else if (truckAudioSource != null && truckAudioSource.isPlaying)
        {
            // If not moving, slowly drop volume to 0 then stop
            truckAudioSource.volume -= Time.deltaTime * 2f; 
            if (truckAudioSource.volume <= 0) truckAudioSource.Stop();
        }
    }

    public void SetTarget(RectTransform targetWaypoint)
    {
        targetPos = targetWaypoint.anchoredPosition + offset;
    
        if (!isMoving && truckAudioSource != null)
        {
            // 4. Reset volume before playing again
            truckAudioSource.volume = originalVolume;
            truckAudioSource.loop = true;
            truckAudioSource.Play();
        }

        isMoving = true;
        if (vibrationScript != null) vibrationScript.shakeAmount = 0.05f; 
    }
    void OnArrived()
    {
        isMoving = false;
        
        // Stop the heavy shaking and switch to idle rumble
        if (vibrationScript != null) vibrationScript.shakeAmount = 0.01f;
        
    }
}