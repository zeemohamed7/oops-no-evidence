using UnityEngine;

public class VehicleVibration : MonoBehaviour
{
    [Header("Vibration Settings")]
    public float shakeAmount = 0.02f;
    public float shakeSpeed = 50f;   
    
    private Vector3 _originalPos;

    void Start()
    {
        _originalPos = transform.localPosition;
    }

    void Update()
    {   
        float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0) - 0.5f) * shakeAmount;
        float offsetY = (Mathf.PerlinNoise(0, Time.time * shakeSpeed) - 0.5f) * shakeAmount;

        transform.localPosition = _originalPos + new Vector3(offsetX, offsetY, 0);
    }
}