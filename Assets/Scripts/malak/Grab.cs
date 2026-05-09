using UnityEngine;
using UnityEngine.InputSystem;
public class Grab : MonoBehaviour
{
    [SerializeField] private PlayerAnimationDriver animationDriver;
    //new
    [Header("Input")]
    public InputActionReference grabAction;
    //
    [Header("Setup")]
    public Transform holdPoint;
    public float grabRange = 2f;

    private GameObject heldObject;
    private FixedJoint joint;

    private TopDownPlayerController playerController;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
    }

    void Update()
    {
        if (grabAction == null || grabAction.action == null)
            return;

        if (grabAction.action.WasPressedThisFrame())
        {
            if (heldObject == null)
                TryGrab();
            else
                Drop();
        }
    }

    //
    void OnEnable()
    {
        if (grabAction != null && grabAction.action != null)
            grabAction.action.Enable();
    }

    void OnDisable()
    {
        if (grabAction != null && grabAction.action != null)
            grabAction.action.Disable();
    }
    //

    void TryGrab()
    {
        Debug.Log("Trying to grab...");
        Collider[] hits = Physics.OverlapSphere(holdPoint.position, grabRange);

        foreach (var hit in hits)
        {
            GrabbableObject grabbable = hit.GetComponent<GrabbableObject>();
            if (grabbable != null)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb == null) continue;

                // Check if the object is already taken
                if (!grabbable.TryGrab(gameObject))
                {
                    Debug.Log("Already grabbed by another player");
                    continue;
                }

                // Successfully found a free object!
                joint = gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = rb;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;

                heldObject = hit.gameObject;

                // Update State
                if (playerController != null) playerController.isCarrying = true;
                if (animationDriver != null) animationDriver.SetCarrying(true);

                Debug.Log("GRAB SUCCESS");
            
                return; // CRITICAL: Stop searching once we have successfully grabbed ONE item.
            }
        }
    }

    void Drop()
    {
        if (heldObject == null) return;

        GrabbableObject grabbable = heldObject.GetComponent<GrabbableObject>();
        if (grabbable != null && grabbable.currentHolder == gameObject)
        {
            grabbable.Release();
        }

        if (joint != null) Destroy(joint);

        heldObject = null;

        // THE FIX: Reset both the controller AND the animation state
        if (playerController != null) playerController.isCarrying = false;
        if (animationDriver != null) animationDriver.SetCarrying(false); 

        Debug.Log("Object Dropped");
    }
    void OnDrawGizmosSelected()
    {
        if (holdPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(holdPoint.position, grabRange);
        }
    }

}

