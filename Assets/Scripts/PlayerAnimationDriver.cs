using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float walkThreshold = 0.01f;

    private Vector3 lastPosition;

    private bool isCarrying;
    private bool hasMop;
    private bool hasFlashlight;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        Vector3 movement = transform.position - lastPosition;
        movement.y = 0f;

        bool isWalking = movement.magnitude > walkThreshold;

        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsCarrying", isCarrying);
        animator.SetBool("HasMop", hasMop);
        animator.SetBool("HasFlashlight", hasFlashlight);

        lastPosition = transform.position;
    }

    public void SetCarrying(bool value)
    {
        isCarrying = value;
    }

    public void SelectMop()
    {
        hasMop = true;
        hasFlashlight = false;
    }

    public void SelectFlashlight()
    {
        hasMop = false;
        hasFlashlight = true;
    }

    public void ClearSelectedItem()
    {
        hasMop = false;
        hasFlashlight = false;
    }

    public void PlayPickUp()
    {
        animator.SetTrigger("PickUp");
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