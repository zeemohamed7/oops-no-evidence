using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isCarrying;
    private bool hasMop;
    private bool hasFlashlight;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void SetWalking(bool value)
    {
        animator.SetBool("IsWalking", value);
    }

    public void SetCarrying(bool value)
    {
        isCarrying = value;
        animator.SetBool("IsCarrying", value);
    }

    public void SelectMop()
    {
        hasMop = true;
        hasFlashlight = false;

        animator.SetBool("HasMop", true);
        animator.SetBool("HasFlashlight", false);
    }

    public void SelectFlashlight()
    {
        hasMop = false;
        hasFlashlight = true;

        animator.SetBool("HasMop", false);
        animator.SetBool("HasFlashlight", true);
    }

    public void ClearSelectedItem()
    {
        hasMop = false;
        hasFlashlight = false;

        animator.SetBool("HasMop", false);
        animator.SetBool("HasFlashlight", false);
    }

    public void PlayPickUpObject()
    {
        animator.SetTrigger("PickUpObject");
    }

    public void PlayPickUpBody()
    {
        animator.SetTrigger("PickUpBody");
    }

    public void PlayDrop()
    {
        animator.SetTrigger("Drop");
    }

    public void PlayMop()
    {
        if (hasMop && !isCarrying)
            animator.SetTrigger("Mop");
    }

    public void PlayDipMop()
    {
        if (hasMop && !isCarrying)
            animator.SetTrigger("DipMop");
    }
}