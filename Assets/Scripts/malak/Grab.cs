using UnityEngine;
using UnityEngine.InputSystem;

public class Grab : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference grabAction;

    [Header("Setup")]
    public Transform holdPoint;
    public Transform chestHoldPoint;
    public float grabRange = 2f;
    
    [Header("Carrying Tweaks")]
    [Tooltip("How far forward in front of the chest to hold the body so it doesn't clip into your player mesh.")]
    public float bodyForwardOffset = 0.5f;

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
    private Animator playerAnimator; // Reference to track player reach animations
    private Vector3 cachedHoldPoint;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
        playerAnimator = GetComponentInChildren<Animator>(); // Cache the player's animator controller
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
        // Dynamic Destination Pick: Track chest transform coordinates for ragdolls, default to arm holdPoint for items
        if (heldGrabbable != null && heldGrabbable.isRagdoll && chestHoldPoint != null)
        {
            // FIX: Add transform.forward multiplication multiplied by your bodyForwardOffset value 
            // This pushes the physical connection point out in front of the player's culling volume boundary.
            cachedHoldPoint = chestHoldPoint.position + (transform.forward * bodyForwardOffset);
        }
        else
        {
            cachedHoldPoint = holdPoint.position;
        }

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

        // FIX: Choose the search origin based on the target type to avoid physics snapping gaps
        // We cast from the chest pivot if we have one available, otherwise default to the hand point.
        Vector3 searchPosition = (chestHoldPoint != null) ? chestHoldPoint.position : holdPoint.position;

        Collider[] hits = Physics.OverlapSphere(
            searchPosition, grabRange, ~0, QueryTriggerInteraction.Collide);

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

            // Trigger the correct animation based on what kind of object we are targeting
            if (playerAnimator != null)
            {
                if (grabbable.isRagdoll)
                {
                    // Fire the trigger for the heavy deadbody lift state (Lifting body)
                    playerAnimator.SetTrigger("PickUpBody");
                }
                else
                {
                    // Fire the trigger for normal props/boxes/items (pick up)
                    playerAnimator.SetTrigger("PickUpObj");
                }

                // Turn on the carry bool so the movement blend trees switch to carrying locomotion
                playerAnimator.SetBool("IsCarrying", true);
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

            // Move physics container straight to the dynamic starting anchor coordinate to stop rubber-banding
            Vector3 startHoldPos = grabbable.isRagdoll && chestHoldPoint != null ? (chestHoldPoint.position + (transform.forward * bodyForwardOffset)) : holdPoint.position;
            anchorObject.transform.position = targetRb.worldCenterOfMass;
            anchorRigidbody.MovePosition(startHoldPos);

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

        anchorObject.transform.position = cachedHoldPoint;
        anchorRigidbody.MovePosition(cachedHoldPoint);
        joint = anchorObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = targetRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;

        if (heldGrabbable.grabAnchor != null)
        {
            joint.connectedAnchor = targetRb.transform.InverseTransformPoint(
                heldGrabbable.grabAnchor.position);
        }
        else
        {
            joint.connectedAnchor = Vector3.zero;
        }

        // ─────────────────────────────────────────────────────────────
        // DYNAMIC TUNING LOGIC BASED ON OBJECT TYPE CARRIED (Normal objects or a deadbody)
        // ─────────────────────────────────────────────────────────────
        if (heldGrabbable.isRagdoll)
        {
            // Carrying a Dead Body
            // Lock spatial positions so his chest bone stays pinned to your chest anchor location
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // LOCKED: This forces the torso to stay completely upright 
            // and rotate exactly when your player turns, stopping all upper-body flailing.
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            
            // Maximum spring power on the horizontal axes to keep the body tight to your movement frame
            JointDrive horizontalDragDrive = new JointDrive
            {
                positionSpring = 20000f,
                positionDamper = 2000f,
                maximumForce = Mathf.Infinity
            };

            // Keep the vertical drive at a medium strength so it supports the weight of his upper chest 
            // but still lets the heavy lower legs droop naturally to drag across the tiles.
            JointDrive verticalDragDrive = new JointDrive
            {
                positionSpring = 4000f, 
                positionDamper = 400f,
                maximumForce = Mathf.Infinity
            };

            joint.xDrive = horizontalDragDrive;
            joint.yDrive = verticalDragDrive; 
            joint.zDrive = horizontalDragDrive;
            
            joint.enableCollision = true; 
        }
        else
        {
            // Carrying Regular Objects (Crates, Barrels, Items)
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            JointDrive rigidDrive = new JointDrive
            {
                positionSpring = 20000f,
                positionDamper = 2000f,
                maximumForce = Mathf.Infinity
            };

            joint.xDrive = rigidDrive;
            joint.yDrive = rigidDrive;
            joint.zDrive = rigidDrive;
            
            joint.enableCollision = true; // Let props bump into walls normally
        }

        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.02f;
        joint.projectionAngle = 1f;
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

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsCarrying", false);
        }
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

        if (chestHoldPoint != null)
        {
            Gizmos.color = Color.magenta;
            // Draw a second gizmo sphere showing exactly where the body will be held with the offset applied
            Vector3 visualOffsetPos = chestHoldPoint.position + (transform.forward * bodyForwardOffset);
            Gizmos.DrawWireSphere(visualOffsetPos, 0.2f);
        }
    }
}