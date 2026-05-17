using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isCarryingBody;
    private bool isCarryingObject;
    private bool hasMop;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void SetWalking(bool value)
    {
        animator.SetBool("IsWalking", value);
    }

    public void SetCarryingObject(bool value)
    {
        isCarryingObject = value;
        animator.SetBool("IsCarryingObject", value);
    }

    public void SetCarryingBody(bool value)
    {
        isCarryingBody = value;
        animator.SetBool("IsCarryingBody", value);
    }

    public void SelectMop()
    {
        hasMop = true;
        animator.SetBool("HasMop", true);
        animator.SetBool("HasTool", false);
        animator.SetBool("HasFlashlight", false);
    }

    public void SelectTool()
    {
        hasMop = false;
        animator.SetBool("HasTool", true);
        animator.SetBool("HasMop", false);
    }

    public void ClearTool()
    {
        animator.SetBool("HasTool", false);
    }

    public void ClearSelectedItem()
    {
        hasMop = false;
        animator.SetBool("HasMop", false);
        animator.SetBool("HasTool", false);
        animator.SetBool("HasFlashlight", false);
    }

    public void PlayPickUpObject()
    {
        animator.ResetTrigger("PickUpBody");
        animator.SetTrigger("PickUpObject");
    }

    public void PlayPickUpBody()
    {
        animator.ResetTrigger("PickUpObject");
        animator.SetTrigger("PickUpBody");
    }

    public void PlayDrop()
    {
        animator.SetTrigger("Drop");
    }

    public void PlayMop()
    {
        if (hasMop && !isCarryingBody && !isCarryingObject)
            animator.SetTrigger("Mop");
    }

    public void PlayDipMop()
    {
        if (hasMop && !isCarryingBody && !isCarryingObject)
            animator.SetTrigger("DipMop");
    }
}