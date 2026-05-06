using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isCarrying;
    private bool hasMop;
    private bool hasTool;


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
        hasTool = false;

        animator.SetBool("HasMop", true);
        animator.SetBool("HasFlashlight", false);
    }

    public void SelectTool()
    {
        animator.SetBool("HasTool", true);
    }

    public void ClearTool()
    {
        animator.SetBool("HasTool", false);
    }


    public void ClearSelectedItem()
    {
        hasMop = false;
        hasTool = false;

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