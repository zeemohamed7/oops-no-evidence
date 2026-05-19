using UnityEngine;
using UnityEngine.InputSystem;

public class Grab : MonoBehaviour
{
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

    // 🟢 LOCAL INPUT REFERENCES
    private PlayerInput playerInput;
    private InputAction localGrabAction;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
        playerAnimator = GetComponentInChildren<Animator>(); 
    
        playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            // 🟢 Change this string to match your action asset row name ("Interact" or your specific button row)
            localGrabAction = playerInput.actions.FindAction("Interact");
        
            // 🟢 FORCE WAKE UP: Tell Unity to explicitly listen to this thread right now
            localGrabAction?.Enable(); 
        }

        CreateAnchor();
    }
    void CreateAnchor()
    {
        anchorObject = new GameObject("GrabAnchor");
        anchorRigidbody = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic = true;
        anchorRigidbody.useGravity = false;
        anchorRigidbody.detectCollisions = false;
        DontDestroyOnLoad(anchorObject);
    }

    // 🟢 Cleaned up to let GameManager manage action map switching states globally
    void OnEnable() { }
    void OnDisable() { }

    void Update()
    {
        // 🛡️ STATE SHIELD: Disable grabbing when not playing (or in lobby)
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            // Force drop anything if it somehow got stuck across scene loads
            if (heldObject != null) Drop(); 
            return;
        }

        // Force both weapons and ragdolls to look for the chest anchor point
        if (heldGrabbable != null && chestHoldPoint != null)
        {
            // Both weapons and deadbodies will now snap forward relative to your chest pivot
            cachedHoldPoint = chestHoldPoint.position + (transform.forward * bodyForwardOffset);
        }
        else
        {
            // Fail-safe: Defaults to regular hand hold point if chestHoldPoint is left empty in Inspector
            cachedHoldPoint = holdPoint != null ? holdPoint.position : transform.position;
        }

        // Prevent infinite spazzing
        if (heldObject != null && anchorObject != null)
        {
            float currentDistance = Vector3.Distance(heldRigidbody.worldCenterOfMass, cachedHoldPoint);

            // Body got stuck behind wall
            if (currentDistance > 2.2f)
            {
                Debug.Log("Body stuck — dropping");
                Drop();
                return;
            }

            // Minor correction only
            if (currentDistance > 1.2f)
            {
                anchorObject.transform.position = Vector3.Lerp(
                    anchorObject.transform.position,
                    cachedHoldPoint,
                    Time.deltaTime * 15f
                );
            }
        }

        if (localGrabAction == null) return;

        // 🟢 FIXED: Only polls the isolated input track assigned to this specific player device track
        if (localGrabAction.WasPressedThisFrame())
        {
            Debug.Log($"[GRAB BUTTON CLICK] {gameObject.name} pressed the grab key/button!");
            if (heldObject == null) TryGrab();
            else Drop();
        }
    }

    void FixedUpdate()
    {
        if (heldRigidbody == null) return;

        float dist = Vector3.Distance(heldRigidbody.worldCenterOfMass, cachedHoldPoint);

        // Body stuck on wall
        if (dist > 1.5f)
            return;

        anchorRigidbody.MovePosition(cachedHoldPoint);

        if (!heldRigidbody.isKinematic && heldRigidbody.linearVelocity.magnitude > 8f)
        {
            heldRigidbody.linearVelocity = heldRigidbody.linearVelocity.normalized * 8f;
        }
    }

    void TryGrab()
    {
        Debug.Log("Trying to grab...");

        Vector3 searchPosition = (chestHoldPoint != null) ? chestHoldPoint.position : holdPoint.position;

        Collider[] hits = Physics.OverlapSphere(searchPosition, grabRange, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) continue;

            // ─────────────────────────────────────────────────────────────
            // 🟢 ALLOW MULTIPLE PLAYERS TO GRAB IF IT'S A RAGDOLL BODY
            // ─────────────────────────────────────────────────────────────
            if (!grabbable.isRagdoll)
            {
                // Normal objects (weapons/crates) still only allow 1 person
                if (!grabbable.TryGrab(gameObject))
                {
                    Debug.Log("Already grabbed by another player");
                    continue;
                }
            }
            else
            {
                // 🟢 FIXED: Skip grabbable.TryGrab completely for ragdolls so it doesn't break player 1's joints!
                // We track registration directly through our clean list in DeadbodyCarry instead.
                DeadbodyCarry carryScript = grabbable.GetComponentInParent<DeadbodyCarry>();
                if (carryScript != null)
                {
                    carryScript.RegisterPlayer(gameObject);
                }
            }

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

            // Trigger animations
            if (playerAnimator != null)
            {
                if (grabbable.isRagdoll)
                    playerAnimator.SetTrigger("PickUpBody");
                else
                    playerAnimator.SetTrigger("PickUpObj");

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

            Vector3 startHoldPos = grabbable.isRagdoll && chestHoldPoint != null ? (chestHoldPoint.position + (transform.forward * bodyForwardOffset)) : holdPoint.position;
            anchorObject.transform.position = targetRb.worldCenterOfMass;
            anchorRigidbody.MovePosition(startHoldPos);

            AttachJoint(targetRb);

            // ─────────────────────────────────────────────────────────────
            // 🟢 SET CARRY WEIGHT PENALTIES BASED ON DYNAMIC COOP LIFTER COUNT
            // ─────────────────────────────────────────────────────────────
            if (playerController != null)
            {
                if (heldGrabbable.isRagdoll)
                {
                    playerController.isCarrying = true; 
                    
                    // Fetch speed parameters based on how many people are lifting right now
                    DeadbodyCarry carryScript = heldObject.GetComponentInParent<DeadbodyCarry>();
                    if (carryScript != null)
                    {
                        playerController.SetCarryWeight(carryScript.GetPenaltyForPlayerCount());
                    }
                }
                else
                {
                    playerController.isCarrying = false;
                }
            }

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Collider[] heldColliders = heldGrabbable.GetComponentsInChildren<Collider>();
                foreach (var col in heldColliders)
                {
                    if (col != null) Physics.IgnoreCollision(cc, col, true);
                }
            }

            Debug.Log($"GRAB SUCCESS — bone: {targetRb.name}");
            return;
        }

        Debug.Log("No grabbable object in range");
    }

    void Drop()
    {
        if (heldObject == null) return;

        // ─────────────────────────────────────────────────────────────
        // 🟢 UNREGISTER FROM WEIGHT CALCULATION SYSTEMS ON DROP
        // ─────────────────────────────────────────────────────────────
        if (heldGrabbable != null && heldGrabbable.isRagdoll)
        {
            DeadbodyCarry carryScript = heldObject.GetComponentInParent<DeadbodyCarry>();
            if (carryScript != null)
            {
                carryScript.UnregisterPlayer(gameObject);
                
                // 🟢 Recalculate and scale speeds for any remaining player left holding the weight
                carryScript.UpdateBodyWeight();        
            }
        }
        else if (heldGrabbable != null && heldGrabbable.currentHolder == gameObject)
        {
            heldGrabbable.Release();
        }

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        if (heldGrabbable != null)
        {
            Rigidbody[] allBones = heldGrabbable.GetComponentsInChildren<Rigidbody>();
            foreach (var rb in allBones)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        if (heldGrabbable != null && heldGrabbable.isRagdoll)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Collider[] bodyColliders = heldGrabbable.GetComponentsInChildren<Collider>();
                foreach (var col in bodyColliders)
                {
                    if (col != null) Physics.IgnoreCollision(cc, col, false);
                }
            }
        }

        heldObject = null;
        heldRigidbody = null;
        heldGrabbable = null;

        if (playerController != null)
            playerController.ClearCarryPenalty(); // 🟢 Safely clear speed penalty entirely back to base attributes

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsCarrying", false);
        }
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
            joint.connectedAnchor = targetRb.transform.InverseTransformPoint(heldGrabbable.grabAnchor.position);
        }
        else
        {
            joint.connectedAnchor = Vector3.zero;
        }

        // ─────────────────────────────────────────────────────────────
        // CONFIGURABLE JOINT SPRING TUNING PATHWAY LOOPS
        // ─────────────────────────────────────────────────────────────
        if (heldGrabbable.isRagdoll)
        {
            // Carrying a Dead Body
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // LOCKED: Keeps the torso upright with your player frame rotation, stopping flailing.
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            
            JointDrive horizontalDragDrive = new JointDrive
            {
                positionSpring = 20000f,
                positionDamper = 2000f,
                maximumForce = Mathf.Infinity
            };

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
            
            joint.enableCollision = true; 
        }

        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.02f;
        joint.projectionAngle = 1f;
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
            Vector3 visualOffsetPos = chestHoldPoint.position + (transform.forward * bodyForwardOffset);
            Gizmos.DrawWireSphere(visualOffsetPos, 0.2f);
        }
    }
}