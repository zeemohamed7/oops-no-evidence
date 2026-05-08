using UnityEngine;

public class BreathingController : MonoBehaviour
{
    public AudioSource breathingSource;

    void Update()
    {
        //just now for testing
        if (Input.GetKeyDown(KeyCode.B))
            StartBreathing();

        if (Input.GetKeyDown(KeyCode.N))
            StopBreathing();
    }
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
