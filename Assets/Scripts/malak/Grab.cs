using UnityEngine;
using UnityEngine.InputSystem;
public class Grab : MonoBehaviour
{
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

        Debug.Log("Objects found in range: " + hits.Length);

        foreach (var hit in hits)
        {
            Debug.Log("Found: " + hit.name + " | Tag: " + hit.tag);

            //here
            GrabbableObject grabbable = hit.GetComponent<GrabbableObject>();
            if (grabbable != null) //here
            {
                Debug.Log("Grabbable object detected!");

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                //GrabbableObject grabbable = hit.GetComponent<GrabbableObject>();

                if (rb == null)
                {
                    Debug.LogError("Object has NO Rigidbody!");
                    continue;//
                }



                if (grabbable != null)
                {
                    if (!grabbable.TryGrab(gameObject))
                    {
                        Debug.Log("Already grabbed by another player");
                        continue;
                    }
                }

                joint = gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = rb;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;

                heldObject = hit.gameObject;

                if (playerController != null)
                    playerController.isCarrying = true;

                Debug.Log("GRAB SUCCESS");
                continue; //it was return
            }
        }

        Debug.Log("No grabbable object in range");
    }

    void Drop()
    {
        if (heldObject == null)
            return;

        GrabbableObject grabbable = heldObject.GetComponent<GrabbableObject>();
        if (grabbable != null && grabbable.currentHolder == gameObject)
        {
            grabbable.Release();
        }

        if (joint != null)
            Destroy(joint);

        heldObject = null;

        if (playerController != null)
            playerController.isCarrying = false;
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

