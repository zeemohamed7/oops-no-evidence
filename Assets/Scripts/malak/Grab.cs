using UnityEngine;
using UnityEngine.InputSystem;

public class Grab : MonoBehaviour
{
    [SerializeField] private PlayerAnimationDriver animationDriver;
    //new
    [Header("Input")]
    public InputActionReference grabAction;

    [Header("Setup")]
    public Transform holdPoint;
    public float grabRange = 2f;

    [Header("Joint Tuning")]
    // Very high spring = bone snaps to holdPoint fast without flying
    public float jointSpring   = 5000f;
    // High damper = kills oscillation so it doesn't bounce or overshoot
    public float jointDamper   = 500f;
    // Max force the joint can apply per tick — cap prevents explosion on fast moves
    public float jointMaxForce = 10000f;

    private GameObject anchorObject;        // invisible kinematic anchor that follows holdPoint
    private Rigidbody  anchorRigidbody;
    private ConfigurableJoint joint;

    private GameObject      heldObject;
    private Rigidbody       heldRigidbody;
    private GrabbableObject heldGrabbable;

    private TopDownPlayerController playerController;

    //new 2
    private Vector3 cachedHoldPoint;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
        CreateAnchor();
    }

    void CreateAnchor()
    {
        anchorObject           = new GameObject("GrabAnchor");
        anchorRigidbody        = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic   = true;   // anchor is immovable by physics
        anchorRigidbody.useGravity    = false;
        DontDestroyOnLoad(anchorObject);        // persist across scene loads if needed
    }

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

    void Update()
    {
        //new
        cachedHoldPoint = holdPoint.position;
        if (grabAction == null || grabAction.action == null) return;
        if (grabAction.action.WasPressedThisFrame())
        {
            if (heldObject == null) TryGrab();
            else Drop();
        }
    }

    void FixedUpdate()
    {
        //new
        if (heldRigidbody == null) return;
        anchorRigidbody.MovePosition(cachedHoldPoint);

        if (heldRigidbody.linearVelocity.magnitude > 8f)
            heldRigidbody.linearVelocity =
                heldRigidbody.linearVelocity.normalized * 8f;
    }

    void TryGrab()
    {
        Debug.Log("Trying to grab...");

        Collider[] hits = Physics.OverlapSphere(
            holdPoint.position, grabRange, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) continue;

            Rigidbody targetRb = null;
            Transform anchor   = grabbable.grabAnchor;

            if (anchor != null)
                targetRb = anchor.GetComponentInParent<Rigidbody>();

            if (targetRb == null)
                targetRb = hit.attachedRigidbody;

            if (targetRb == null)
            {
                Debug.LogError("No Rigidbody found to grab.");
                continue;
            }

            if (!grabbable.TryGrab(gameObject))
            {
                Debug.Log("Already grabbed by another player");
                continue;
            }

            heldGrabbable = grabbable;
            heldObject    = grabbable.gameObject;
            heldRigidbody = targetRb;

            
            Rigidbody[] allBones = heldGrabbable.GetComponentsInChildren<Rigidbody>();
            foreach (var bone in allBones)
            {
                bone.linearVelocity  = Vector3.zero;
                bone.angularVelocity = Vector3.zero;
            }

            
            anchorObject.transform.position = targetRb.worldCenterOfMass;
            anchorRigidbody.MovePosition(targetRb.worldCenterOfMass);

            AttachJoint(targetRb);

            if (playerController != null)
                playerController.isCarrying = true;
             if (animationDriver != null) animationDriver.SetCarrying(true);


            Debug.Log($"GRAB SUCCESS — bone: {targetRb.name}");
            //new
            Collider[] bodyColliders = heldObject.GetComponentsInChildren<Collider>();
            foreach (var col in bodyColliders)
            {
                col.gameObject.layer = LayerMask.NameToLayer("HeldBody");
            }
            //
            return;
        }
    }

    void AttachJoint(Rigidbody targetRb)
    {
        if (joint != null)
            Destroy(joint);

        anchorObject.transform.position = holdPoint.position;
        anchorRigidbody.MovePosition(holdPoint.position);
        joint = anchorObject.AddComponent<ConfigurableJoint>();

        
        // grabbed object becomes connectedBody
        joint.connectedBody = targetRb;

        // FIX FOR FLOATING GAP
        joint.autoConfigureConnectedAnchor = false;

        // Connect centers directly
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = Vector3.zero;

        // POSITIONAL MOVEMENT
        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        // LOCK ROTATION
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // STRONGER / TIGHTER DRIVE
        JointDrive drive = new JointDrive
        {
            positionSpring = 12000f,
            positionDamper = 1200f,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        // Prevent crazy stretching
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.02f;
        joint.projectionAngle = 1f;

        // Keep world collisions
        joint.enableCollision = true;

        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
    }

    void Drop()
    {
        if (heldObject == null) return;

        // Release ownership
        if (heldGrabbable != null &&
            heldGrabbable.currentHolder == gameObject)
        {
            heldGrabbable.Release();
        }

        // Destroy joint safely
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        // Reset all ragdoll velocities
        if (heldGrabbable != null)
        {
            Rigidbody[] allBones =
                heldGrabbable.GetComponentsInChildren<Rigidbody>();

            foreach (var rb in allBones)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Restore layers
        Collider[] bodyColliders =
            heldObject.GetComponentsInChildren<Collider>();

        foreach (var col in bodyColliders)
        {
            col.gameObject.layer =
                LayerMask.NameToLayer("Default");
        }

        // CLEAR REFERENCES
        heldObject = null;
        heldRigidbody = null;
        heldGrabbable = null;

        // THE FIX: Reset both the controller AND the animation state
        if (playerController != null) playerController.isCarrying = false;
        if (animationDriver != null) animationDriver.SetCarrying(false); 

        Debug.Log("Object Dropped");
    }


    void OnDestroy()
    {
        if (anchorObject != null)
            Destroy(anchorObject);
    }

    void OnDrawGizmosSelected()
    {
        if (holdPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(holdPoint.position, grabRange);
    }
}