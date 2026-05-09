using UnityEngine;
using UnityEngine.InputSystem;

public class Grab : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference grabAction;

    [Header("Setup")]
    public Transform holdPoint;
    public float grabRange = 2f;

    [Header("Joint Tuning")]
    public float jointSpring = 5000f;
    public float jointDamper = 500f;
    public float jointMaxForce = 10000f;

    private GameObject anchorObject;
    private Rigidbody anchorRigidbody;
    private ConfigurableJoint joint;

    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private GrabbableObject heldGrabbable;

    private TopDownPlayerController playerController;
    private Vector3 cachedHoldPoint;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
        CreateAnchor();
    }

    void CreateAnchor()
    {
        anchorObject = new GameObject("GrabAnchor");
        anchorRigidbody = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic = true;
        anchorRigidbody.useGravity = false;
        DontDestroyOnLoad(anchorObject);
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
        if (heldRigidbody == null) return;

        anchorRigidbody.MovePosition(cachedHoldPoint);

        // Only clamp velocity on non-kinematic bones
        if (!heldRigidbody.isKinematic &&
            heldRigidbody.linearVelocity.magnitude > 8f)
        {
            heldRigidbody.linearVelocity =
                heldRigidbody.linearVelocity.normalized * 8f;
        }
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
            Transform anchor = grabbable.grabAnchor;

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
            heldObject = grabbable.gameObject;
            heldRigidbody = targetRb;

            // Kill velocity on all non-kinematic bones at grab moment
            Rigidbody[] allBones = heldGrabbable.GetComponentsInChildren<Rigidbody>();
            foreach (var bone in allBones)
            {
                if (!bone.isKinematic)
                {
                    bone.linearVelocity = Vector3.zero;
                    bone.angularVelocity = Vector3.zero;
                }
            }

            anchorObject.transform.position = targetRb.worldCenterOfMass;
            anchorRigidbody.MovePosition(targetRb.worldCenterOfMass);

            AttachJoint(targetRb);

            if (playerController != null)
                playerController.isCarrying = true;

            // Only ignore collision for ragdoll bodies, not regular items like boxes
            if (heldGrabbable.isRagdoll)
            {
                CharacterController cc = GetComponent<CharacterController>();
                if (cc != null)
                {
                    Collider[] bodyColliders =
                        heldGrabbable.GetComponentsInChildren<Collider>();
                    foreach (var col in bodyColliders)
                        Physics.IgnoreCollision(cc, col, true);
                }
            }

            Debug.Log($"GRAB SUCCESS — bone: {targetRb.name}");
            return;
        }

        Debug.Log("No grabbable object in range");
    }

    void AttachJoint(Rigidbody targetRb)
    {
        if (joint != null)
            Destroy(joint);

        anchorObject.transform.position = holdPoint.position;
        anchorRigidbody.MovePosition(holdPoint.position);
        joint = anchorObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = targetRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;

        // Connect at the exact grab anchor position (e.g. the hand)
        // not at the parent bone center
        if (heldGrabbable.grabAnchor != null)
        {
            joint.connectedAnchor = targetRb.transform.InverseTransformPoint(
                heldGrabbable.grabAnchor.position);
        }
        else
        {
            joint.connectedAnchor = Vector3.zero;
        }

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        // Free rotation so body tumbles naturally when dragged
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // Horizontal drive — pulls object along the ground
        JointDrive horizontalDrive = new JointDrive
        {
            positionSpring = 12000f,
            positionDamper = 1200f,
            maximumForce = Mathf.Infinity
        };

        // Ragdoll: no vertical lift so body stays on ground
        // Regular item: full vertical lift so box/item rises to hand
        JointDrive verticalDrive = new JointDrive
        {
            positionSpring = heldGrabbable.isRagdoll ? 2000f : 12000f,
            positionDamper = 1200f,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = horizontalDrive;
        joint.yDrive = verticalDrive;
        joint.zDrive = horizontalDrive;

        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.02f;
        joint.projectionAngle = 1f;
        joint.enableCollision = true;
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
    }

    void Drop()
    {
        if (heldObject == null) return;

        if (heldGrabbable != null &&
            heldGrabbable.currentHolder == gameObject)
        {
            heldGrabbable.Release();
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        // Kill velocity on all non-kinematic bones on drop
        if (heldGrabbable != null)
        {
            Rigidbody[] allBones =
                heldGrabbable.GetComponentsInChildren<Rigidbody>();

            foreach (var rb in allBones)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        // Restore collision only if it was a ragdoll
        if (heldGrabbable != null && heldGrabbable.isRagdoll)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Collider[] bodyColliders =
                    heldGrabbable.GetComponentsInChildren<Collider>();
                foreach (var col in bodyColliders)
                    Physics.IgnoreCollision(cc, col, false);
            }
        }

        heldObject = null;
        heldRigidbody = null;
        heldGrabbable = null;

        if (playerController != null)
            playerController.isCarrying = false;
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