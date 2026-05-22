using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isCarrying;
    private bool hasMop;
    private bool isMopping;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isMopping && animator.GetBool("IsMopping") && !Input.anyKey)
        {
            SetMopping(false);
        }
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
        Debug.Log("ANIM: SelectMop called");

        if (isCarrying) return;

        hasMop = true;
        animator.SetBool("HasMop", true);
        animator.SetBool("HasTool", false);
        animator.SetBool("HasFlashlight", false);
    }

    public void SelectTool()
    {
        if (isCarrying) return;

        SetMopping(false);

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
        SetMopping(false);

        hasMop = false;
        animator.SetBool("HasMop", false);
        animator.SetBool("HasTool", false);
        animator.SetBool("HasFlashlight", false);
    }

    public void PlayPickUpBody()
    {
        animator.SetTrigger("PickUpBody");
    }

    public void PlayDrop()
    {
        animator.SetTrigger("Drop");
    }

    // public void PlayMop()
    // {
    //     Debug.Log("ANIM: PlayMop called. hasMop = " + hasMop + ", isCarrying = " + isCarrying);

    //     if (hasMop && !isCarrying)
    //         animator.SetTrigger("Mop");
    // }

    public void PlayMop()
    {
        SetMopping(true);
    }

    public void StopMop()
    {
        SetMopping(false);
    }
    
    public void SetMopping(bool value)
    {
        if (!hasMop || isCarrying)
            value = false;

        if (isMopping == value) return;

        isMopping = value;
        animator.SetBool("IsMopping", value);
    }

    public void PlayDipMop()
    {
        if (hasMop && !isCarrying)
            animator.SetTrigger("DipMop");
    }
    public void PlayUseTool()
    {
        if (!isCarrying)
            animator.SetTrigger("UseTool");
    }
}