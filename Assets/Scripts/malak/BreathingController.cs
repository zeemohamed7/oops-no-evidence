using UnityEngine;

public class BreathingController : MonoBehaviour
{
    public AudioSource breathingSource;

    void Start()
    {
        breathingSource.Stop();
    }

    public void StartBreathing()
    {
        if (!breathingSource.isPlaying)
            breathingSource.Play();
    }

    public void StopBreathing()
    {
        breathingSource.Stop();
    }
}