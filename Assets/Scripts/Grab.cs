using UnityEngine;
using UnityEngine.InputSystem;

public class Grab : MonoBehaviour
{
    [Header("Setup")]
    public Transform holdPoint;
    public Transform chestHoldPoint;
    public float grabRange = 2f;
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
    private Animator playerAnimator; 
    private Vector3 cachedHoldPoint;
    private ToolInventory toolInventory;

    private PlayerInput playerInput;
    private InputAction localGrabAction;

    void Start()
    {
        playerController = GetComponent<TopDownPlayerController>();
        playerAnimator = GetComponentInChildren<Animator>(); 
        toolInventory = GetComponent<ToolInventory>() ?? GetComponentInChildren<ToolInventory>();
    
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
        // spawn an empty, invisible object to use as a proxy handle
        anchorObject = new GameObject("GrabAnchor");
        // add a rigidbody so the physics engine can hook a joint to it
        anchorRigidbody = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic = true;
        // turn off gravity so the invisible anchor doesn't fall through the map
        anchorRigidbody.useGravity = false;
        // turn off collisions so it never bumps into walls or trips the player
        anchorRigidbody.detectCollisions = false;
        // keep the anchor alive when transitioning between levels or scenes
        DontDestroyOnLoad(anchorObject);
    }
    void Update()
    {
        // break if game isn't running or in lobby
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            if (heldObject != null) Drop(); 
            return;
        }

        // offset the hold point forward so stuff doesn't clip into player mesh
        if (heldGrabbable != null && chestHoldPoint != null)
        {
            cachedHoldPoint = chestHoldPoint.position + (transform.forward * bodyForwardOffset);
        }
        else
        {
            cachedHoldPoint = holdPoint != null ? holdPoint.position : transform.position;
        }

        if (heldObject != null && anchorObject != null && heldGrabbable != null)
        {
            // use the root object position for ragdolls instead of bone center
            Vector3 targetReferencePos = heldGrabbable.isRagdoll ? 
                heldObject.transform.position : 
                heldRigidbody.worldCenterOfMass;

            float currentDistance = Vector3.Distance(targetReferencePos, cachedHoldPoint);
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

        if (localGrabAction.WasPressedThisFrame())
        {
            if (heldObject == null) TryGrab();
            else Drop();
        }
    }

    void FixedUpdate()
    {
        if (heldRigidbody == null || anchorRigidbody == null) return;

        float dist = Vector3.Distance(heldRigidbody.worldCenterOfMass, cachedHoldPoint);

        if (dist > 1.5f) return;

        // lerp anchor position to fix high frequency jittering
        Vector3 smoothedTargetPos = Vector3.Lerp(anchorRigidbody.position, cachedHoldPoint, Time.fixedDeltaTime * 20f);
        anchorRigidbody.MovePosition(smoothedTargetPos);

        if (!heldRigidbody.isKinematic && heldRigidbody.linearVelocity.magnitude > 8f)
        {
            heldRigidbody.linearVelocity = heldRigidbody.linearVelocity.normalized * 8f;
        }
    }

    void TryGrab()
    {
        Vector3 searchPosition = (chestHoldPoint != null) ? chestHoldPoint.position : holdPoint.position;
        Collider[] hits = Physics.OverlapSphere(searchPosition, grabRange, ~0, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            // skip hitting other players or yourself
            if (hit.gameObject.CompareTag("Player") && hit.gameObject != gameObject) continue;
            if (hit.transform.root.gameObject.CompareTag("Player") && hit.transform.root.gameObject != gameObject) continue;

            GrabbableObject grabbable = hit.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) continue;

            if (!grabbable.isRagdoll)
            {
                if (!grabbable.TryGrab(gameObject)) continue;
            }
            else
            {
                // register player to carry script for weight and speed math
                DeadbodyCarry carryScript = grabbable.GetComponentInParent<DeadbodyCarry>();
                if (carryScript != null)
                {
                    carryScript.RegisterPlayer(gameObject);
                }
            }

            // lock onto the exact bone rigidbody that was hit
            Rigidbody targetRb = hit.attachedRigidbody;

            if (targetRb == null)
            {
                targetRb = hit.GetComponent<Rigidbody>() ?? hit.GetComponentInParent<Rigidbody>();
            }

            if (targetRb == null || targetRb.isKinematic) continue;

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

            // kill velocity on bones right when you grab them
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
                        // apply heavy movement penalty based on current carrier count
                        playerController.SetCarryWeight(carryScript.GetPenaltyForPlayerCount());
                    }
                }
                else
                {
                    playerController.isCarrying = false;
                }
            }

            // stop player controller from colliding with the body parts
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                Collider[] heldColliders = heldGrabbable.GetComponentsInChildren<Collider>();
                foreach (var col in heldColliders)
                {
                    if (col != null) Physics.IgnoreCollision(cc, col, true);
                }
            }

            // hide cleaning tools when holding a body or big prop
            if (toolInventory != null)
            {
                toolInventory.isCarryingHeavyObject = true;
            }

            return;
        }
    }

    public void Drop()
    {
        if (heldObject == null) return;

        if (heldGrabbable != null && heldGrabbable.isRagdoll)
        {
            DeadbodyCarry carryScript = heldObject.GetComponentInParent<DeadbodyCarry>();
            if (carryScript != null)
            {
                carryScript.UnregisterPlayer(gameObject);
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
            // turn collisions back on between player and body
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

        // reset player movement speeds back to normal
        if (playerController != null)
            playerController.ClearCarryPenalty(); 

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsCarrying", false);
        }

        // bring back cleaning tools to player hands
        if (toolInventory != null)
        {
            toolInventory.isCarryingHeavyObject = false;
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
        joint.connectedAnchor = Vector3.zero; 

        if (heldGrabbable.isRagdoll)
        {
            // use limited motion with a soft cushion to stop joints fighting each other
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit linearLimit = new SoftJointLimit { limit = 0.1f };
            joint.linearLimit = linearLimit;

            // let joints rotate freely so parts don't stretch or snap out
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            // lower springs so body sags smoothly instead of vibrating violently
            JointDrive bodyDrive = new JointDrive
            {
                positionSpring = 4500f,  
                positionDamper = 350f,   
                maximumForce = Mathf.Infinity
            };

            joint.xDrive = bodyDrive;
            joint.yDrive = bodyDrive; 
            joint.zDrive = bodyDrive;
            
            joint.enableCollision = false; 
        }
        else
        {
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
        joint.projectionDistance = 0.01f; 
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