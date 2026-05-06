using UnityEngine;

public class FootstepController : MonoBehaviour
{
    public AudioSource footstepSource;
    public float stepDelay = 0.5f; // time between steps
    private float timer;

    void Update()
    {
        float move = Input.GetAxis("Horizontal") + Input.GetAxis("Vertical");

        if (move != 0) // player is moving
        {
            timer += Time.deltaTime;

            if (timer >= stepDelay)
            {
                footstepSource.Play();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }
}
