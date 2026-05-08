using UnityEngine;

public class TruckArrival : MonoBehaviour
{
    public Transform startPoint; 
    public Transform stopPoint;
    public float driveSpeed = 5f;
    
    [Header("References")]
    public GameManager gameManager;
    public VehicleVibration vibrationScript;
    public EngineSound engineSoundScript;
    public AudioSource truckAudioSource;
    public AudioClip engineOffClip;

    private bool _isMoving = false;

    void Start()
    {
        if(startPoint != null && stopPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
            _isMoving = true;
        }
    }

    void Update()
    {
        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, stopPoint.position, driveSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, stopPoint.position) < 0.1f)
            {
                OnArrived();
            }
        }
    }
    void OnArrived()
    {
        _isMoving = false;

        if (gameManager != null)
        {
            gameManager.OnTruckStopped(); // To spawn players
        }
        
        if (vibrationScript != null)
        {
            // Lower shake amount
            vibrationScript.shakeAmount = 0.01f; 
        }

        // 2. The EngineSound script will automatically handle the pitch!
        // Since currentSpeed is now 0, the script we wrote will 
        // naturally drop the pitch to 'minPitch' (the idle rumble).

        // 3. Play a "Brake Hiss"
        if (truckAudioSource != null && engineOffClip != null)
        {
            truckAudioSource.PlayOneShot(engineOffClip); 
        }
    }
}