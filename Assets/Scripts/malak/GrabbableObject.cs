using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    [Header("State")]
    public bool isGrabbed = false;
    public GameObject currentHolder;

    [Header("Ragdoll Grab Setup")]
    public bool isRagdoll = false;
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

    // 🟢 UPDATED FOR CO-OP MULTIPLAYER LIFTING
    public bool TryGrab(GameObject player)
    {
        // If it's a dead body ragdoll, completely bypass the single-holder lock!
        if (isRagdoll)
        {
            Debug.Log($"[GRABBABLE] Co-op registration allowed for {player.name} on ragdoll.");
            return true; 
        }

        // 🛑 STANDARD PROP LOGIC: Weapons, crates, items only allow one holder
        if (isGrabbed && currentHolder != player) 
            return false;

        SetHolder(player);
        return true;
    }

    public void Release()
    {
        // Only clear standard item tracking if it's not a multi-user ragdoll
        if (!isRagdoll)
        {
            ClearHolder();
        }
    }

    private void SetHolder(GameObject player)
    {
        isGrabbed = true;
        currentHolder = player;
    }

    private void ClearHolder()
    {
        isGrabbed = false;
        currentHolder = null;
    }
}