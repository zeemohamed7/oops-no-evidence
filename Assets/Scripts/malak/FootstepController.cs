using UnityEngine;
using UnityEngine.InputSystem;

public class FootstepController : MonoBehaviour
{
    public AudioSource footstepSource;
    public float stepDelay = 0.5f;
    public InputActionReference moveAction;
    private float timer;

    void Update()
    {
        Vector2 input = moveAction != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        float move = input.magnitude;

        if (move > 0.1f)
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