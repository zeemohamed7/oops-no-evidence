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
            localGrabAction = playerInput.actions.FindAction("Grab");
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

    // --- Prevent infinite stretching/spazzing ---
    if (heldObject != null && anchorObject != null && heldGrabbable != null)
    {
        // 🟢 FIXED CO-OP DISTANCE CHECK:
        // Track distance from the central object transform root for ragdolls, NOT the individual bone mass point.
        Vector3 targetReferencePos = heldGrabbable.isRagdoll ? 
            heldObject.transform.position : 
            heldRigidbody.worldCenterOfMass;

        float currentDistance = Vector3.Distance(targetReferencePos, cachedHoldPoint);

        // // 🟢 Dynamic Co-Op Distance Tolerance Threshold
        // // Give co-op players extra breathing room (3.5m) so pulling separate limbs doesn't auto-drop!
        // float breakDistance = heldGrabbable.isRagdoll ? 3.5f : 2.2f;
        //
        // if (currentDistance > breakDistance)
        // {
        //     Debug.Log($"Body stuck or pulled too far ({currentDistance}m) — dropping");
        //     Drop();
        //     return;
        // }

        // Minor correction position scaling adjustments
        float lerpThreshold = heldGrabbable.isRagdoll ? 1.8f : 1.2f;
        if (currentDistance > lerpThreshold)
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
    if (heldRigidbody == null || anchorRigidbody == null) return;

    float dist = Vector3.Distance(heldRigidbody.worldCenterOfMass, cachedHoldPoint);

    // Body stuck on wall threshold
    if (dist > 1.5f)
        return;

    // 🟢 CO-OP SMOOTHNESS FIX 4: Smoothly blend the anchor position instead of snapping it.
    // This stops the high-frequency vibration before it can even start!
    Vector3 smoothedTargetPos = Vector3.Lerp(anchorRigidbody.position, cachedHoldPoint, Time.fixedDeltaTime * 20f);
    anchorRigidbody.MovePosition(smoothedTargetPos);

    if (!heldRigidbody.isKinematic && heldRigidbody.linearVelocity.magnitude > 8f)
    {
        heldRigidbody.linearVelocity = heldRigidbody.linearVelocity.normalized * 8f;
    }
}
  void TryGrab()
{
    Debug.Log($"[{gameObject.name}] Trying to grab...");

    Vector3 searchPosition = (chestHoldPoint != null) ? chestHoldPoint.position : holdPoint.position;

    // 🟢 CO-OP FILTER 1: Only check layers that can actually be grabbed (e.g., Ragdolls/Props)
    // Avoid checking '~0' (Everything) so players don't accidentally check each other's bodies!
    // If you don't have a specific layer, keep ~0 but the code below will now filter players out.
    Collider[] hits = Physics.OverlapSphere(searchPosition, grabRange, ~0, QueryTriggerInteraction.Collide);

    foreach (var hit in hits)
    {
        // 🛑 FILTER OUT OTHER PLAYERS: If the hit object is a player, and it's not YOU, skip it immediately!
        if (hit.gameObject.CompareTag("Player") && hit.gameObject != gameObject) continue;
        if (hit.transform.root.gameObject.CompareTag("Player") && hit.transform.root.gameObject != gameObject) continue;

        GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
        if (grabbable == null) continue;

        if (!grabbable.isRagdoll)
        {
            if (!grabbable.TryGrab(gameObject))
            {
                Debug.Log("Already grabbed by another player");
                continue;
            }
        }
        else
        {
            DeadbodyCarry carryScript = grabbable.GetComponentInParent<DeadbodyCarry>();
            if (carryScript != null)
            {
                carryScript.RegisterPlayer(gameObject);
            }
        }

        // 🟢 CO-OP FILTER 2: Target the EXACT specific bone Rigidbody that was hit!
        Rigidbody targetRb = hit.attachedRigidbody;

        // If the specific collider doesn't have an attached Rigidbody on its own transform slot,
        // search upwards into its immediate local parent rather than jumping all the way to the root asset anchor
        if (targetRb == null)
        {
            targetRb = hit.GetComponent<Rigidbody>() ?? hit.GetComponentInParent<Rigidbody>();
        }

        if (targetRb == null || targetRb.isKinematic)
        {
            Debug.LogError($"[GRAB FAIL] Hit collider {hit.name} lacks a valid dynamic Rigidbody bone structure.");
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
        heldRigidbody = targetRb; // Locks securely onto Player 2's specific limb bone target!

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

        if (playerController != null)
        {
            if (heldGrabbable.isRagdoll)
            {
                playerController.isCarrying = true; 
                
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

        // 🟢 Check your console for this log message to see exactly which bone Player 2 secured!
        Debug.Log($"[GRAB MOUNT SUCCESS] {gameObject.name} successfully anchored to bone: {targetRb.name}!");
        return;
    }

    Debug.Log($"[{gameObject.name}] No grabbable object inside overlap radius.");
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

    // void AttachJoint(Rigidbody targetRb)
    // {
    //     if (joint != null)
    //         Destroy(joint);
    //
    //     anchorObject.transform.position = cachedHoldPoint;
    //     anchorRigidbody.MovePosition(cachedHoldPoint);
    //     joint = anchorObject.AddComponent<ConfigurableJoint>();
    //
    //     joint.connectedBody = targetRb;
    //     joint.autoConfigureConnectedAnchor = false;
    //     joint.anchor = Vector3.zero;
    //
    //     if (heldGrabbable.grabAnchor != null)
    //     {
    //         joint.connectedAnchor = targetRb.transform.InverseTransformPoint(heldGrabbable.grabAnchor.position);
    //     }
    //     else
    //     {
    //         joint.connectedAnchor = Vector3.zero;
    //     }
    //
    //     // ─────────────────────────────────────────────────────────────
    //     // CONFIGURABLE JOINT SPRING TUNING PATHWAY LOOPS
    //     // ─────────────────────────────────────────────────────────────
    //     if (heldGrabbable.isRagdoll)
    //     {
    //         // Carrying a Dead Body
    //         joint.xMotion = ConfigurableJointMotion.Locked;
    //         joint.yMotion = ConfigurableJointMotion.Locked;
    //         joint.zMotion = ConfigurableJointMotion.Locked;
    //
    //         // LOCKED: Keeps the torso upright with your player frame rotation, stopping flailing.
    //         joint.angularXMotion = ConfigurableJointMotion.Locked;
    //         joint.angularYMotion = ConfigurableJointMotion.Locked;
    //         joint.angularZMotion = ConfigurableJointMotion.Locked;
    //         
    //         JointDrive horizontalDragDrive = new JointDrive
    //         {
    //             positionSpring = 20000f,
    //             positionDamper = 2000f,
    //             maximumForce = Mathf.Infinity
    //         };
    //
    //         JointDrive verticalDragDrive = new JointDrive
    //         {
    //             positionSpring = 4000f, 
    //             positionDamper = 400f,
    //             maximumForce = Mathf.Infinity
    //         };
    //
    //         joint.xDrive = horizontalDragDrive;
    //         joint.yDrive = verticalDragDrive; 
    //         joint.zDrive = horizontalDragDrive;
    //         
    //         joint.enableCollision = true; 
    //     }
    //     else
    //     {
    //         // Carrying Regular Objects (Crates, Barrels, Items)
    //         joint.xMotion = ConfigurableJointMotion.Locked;
    //         joint.yMotion = ConfigurableJointMotion.Locked;
    //         joint.zMotion = ConfigurableJointMotion.Locked;
    //
    //         joint.angularXMotion = ConfigurableJointMotion.Locked;
    //         joint.angularYMotion = ConfigurableJointMotion.Locked;
    //         joint.angularZMotion = ConfigurableJointMotion.Locked;
    //
    //         JointDrive rigidDrive = new JointDrive
    //         {
    //             positionSpring = 20000f,
    //             positionDamper = 2000f,
    //             maximumForce = Mathf.Infinity
    //         };
    //
    //         joint.xDrive = rigidDrive;
    //         joint.yDrive = rigidDrive;
    //         joint.zDrive = rigidDrive;
    //         
    //         joint.enableCollision = true; 
    //     }
    //
    //     joint.projectionMode = JointProjectionMode.PositionAndRotation;
    //     joint.projectionDistance = 0.02f;
    //     joint.projectionAngle = 1f;
    // }
void AttachJoint(Rigidbody targetRb)
{
    if (joint != null)
        Destroy(joint);

    // 1. Move the invisible anchor object directly to your player's chest/hand hold point
    anchorObject.transform.position = cachedHoldPoint;
    anchorRigidbody.MovePosition(cachedHoldPoint);
    
    joint = anchorObject.AddComponent<ConfigurableJoint>();
    joint.connectedBody = targetRb;

    // 🟢 CO-OP CORRECTION 1: Turn this off so it forces a snap directly to our hold points!
    joint.autoConfigureConnectedAnchor = false; 
    joint.anchor = Vector3.zero;
    joint.connectedAnchor = Vector3.zero; 

    // ─────────────────────────────────────────────────────────────
    // DYNAMIC TUNING LOGIC BASED ON OBJECT TYPE CARRIED
    // ─────────────────────────────────────────────────────────────
    if (heldGrabbable.isRagdoll)
    {
        // 🟢 CO-OP SMOOTHNESS FIX 1: Change from Locked to Limited.
        // This gives the bone a tiny physical buffer room to breathe between players.
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        // Strict limit: Only allow the bone to shift up to 0.1 meters away from your chest point
        SoftJointLimit linearLimit = new SoftJointLimit { limit = 0.1f };
        joint.linearLimit = linearLimit;

        // 🟢 CO-OP SMOOTHNESS FIX 2: Free the angular pathways completely!
        // This lets the spine and limbs twist and flip naturally between players 
        // instead of locking up and forcing the physics engine to vibrate.
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // 🟢 CO-OP SMOOTHNESS FIX 3: Soften the Spring Forces.
        // Lowering the spring values stops the joints from violently snapping the bones back and forth.
        JointDrive bodyDrive = new JointDrive
        {
            positionSpring = 4500f,  // 📉 Was 30000f - way smoother pulling strength
            positionDamper = 350f,   // 📉 Was 1500f  - prevents over-shooting bouncing
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = bodyDrive;
        joint.yDrive = bodyDrive; 
        joint.zDrive = bodyDrive;
        
        // Keep this false so your independent joint tracks don't collide with one another
        joint.enableCollision = false; 
    
    }
    else
    {
        // Carrying Regular Objects (Crates, Barrels, Items)
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        
        if (heldGrabbable.grabAnchor != null)
            joint.connectedAnchor = targetRb.transform.InverseTransformPoint(heldGrabbable.grabAnchor.position);
        else
            joint.connectedAnchor = Vector3.zero;

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
    joint.projectionDistance = 0.01f; // High accuracy snapping
    joint.projectionAngle = 1f;
}
    void OnDestroy()
    {
        if (anchorObject != null)
            Destroy(anchorObject);
    }

    public GameObject GetHeldObject() => heldObject;
    
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