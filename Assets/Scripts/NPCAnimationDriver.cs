using UnityEngine;

public class NPCAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void SetWalking(bool value)
    {
        animator.SetBool("IsWalking", value);
    }

    public void SetRunning(bool value)
    {
        animator.SetBool("IsRunning", value);
    }

    public void PlayLookAround()
    {
        animator.SetTrigger("LookAround");
    }

    public void PlayDeath()
    {
        animator.SetTrigger("Die");
    }
}

