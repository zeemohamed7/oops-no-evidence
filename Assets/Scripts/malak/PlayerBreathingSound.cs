using UnityEngine;

public class PlayerBreathingSound : MonoBehaviour
{
    [SerializeField] private AudioSource breathingAudio;

    public void StartBreathing()
    {
        if (!breathingAudio.isPlaying)
            breathingAudio.Play();
    }

    public void StopBreathing()
    {
        if (breathingAudio.isPlaying)
            breathingAudio.Stop();
    }
}
