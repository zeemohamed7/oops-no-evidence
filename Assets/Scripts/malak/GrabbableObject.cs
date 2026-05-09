using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    [Header("State")]
    public bool isGrabbed = false;
    public GameObject currentHolder;

    [Header("Ragdoll Grab Setup")]
    public Rigidbody mainRigidbody;
    public Transform grabAnchor;

    private Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        
        if (animator != null)
            animator.enabled = false;

        Rigidbody[] bones = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in bones)
        {
            
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        
        CharacterJoint[] joints = GetComponentsInChildren<CharacterJoint>();
        foreach (var j in joints)
            j.enablePreprocessing = false;
    }

    public bool TryGrab(GameObject player)
    {
        if (isGrabbed && currentHolder != player) return false;
        SetHolder(player);
        return true;
    }

    public void Release() => ClearHolder();

    private void SetHolder(GameObject player)
    {
        isGrabbed     = true;
        currentHolder = player;
    }

    private void ClearHolder()
    {
        isGrabbed     = false;
        currentHolder = null;
    }
}