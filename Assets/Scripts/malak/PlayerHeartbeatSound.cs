using UnityEngine;

public class PlayerHeartbeatSound : MonoBehaviour
{
    [SerializeField] private Transform police;
    [SerializeField] private AudioSource heartbeatAudio;

    [Header("Distance")]
    [SerializeField] private float heartbeatStartDistance = 12f;
    [SerializeField] private float closestDistance = 3f;

    [Header("Sound")]
    [SerializeField] private float minVolume = 0.2f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private float minPitch = 1f;
    [SerializeField] private float maxPitch = 1.8f;

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, police.position);

        if (distance <= heartbeatStartDistance)
        {
            if (!heartbeatAudio.isPlaying)
                heartbeatAudio.Play();

            float dangerAmount = 1 - Mathf.InverseLerp(
                closestDistance,
                heartbeatStartDistance,
                distance
            );

            heartbeatAudio.volume = Mathf.Lerp(minVolume, maxVolume, dangerAmount);
            heartbeatAudio.pitch = Mathf.Lerp(minPitch, maxPitch, dangerAmount);
        }
        else
        {
            if (heartbeatAudio.isPlaying)
                heartbeatAudio.Stop();
        }
    }
}
