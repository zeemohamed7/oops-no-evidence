using UnityEngine;
using UnityEngine.InputSystem;
public class Grab : MonoBehaviour
{
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
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject == null)
                TryGrab();
            else
                Drop();
        }
    }

    void TryGrab()
    {
        Debug.Log("Trying to grab...");

        Collider[] hits = Physics.OverlapSphere(holdPoint.position, grabRange);

        Debug.Log("Objects found in range: " + hits.Length);

        foreach (var hit in hits)
        {
            Debug.Log("Found: " + hit.name + " | Tag: " + hit.tag);

            if (hit.CompareTag("Grabbable"))
            {
                Debug.Log("Grabbable object detected!");

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                GrabbableObject grabbable = hit.GetComponent<GrabbableObject>();

                if (rb == null)
                {
                    Debug.LogError("Object has NO Rigidbody!");
                    return;
                }

               
                if (grabbable != null && !grabbable.TryGrab(gameObject))
                {
                    Debug.Log("Already grabbed by another player");
                    return;
                }

                joint = gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = rb;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;

                heldObject = hit.gameObject;

                if (playerController != null)
                    playerController.isCarrying = true;

                Debug.Log("GRAB SUCCESS");
                return;
            }
        }

        Debug.Log("No grabbable object in range");
    }

    void Drop()
    {
        if (heldObject == null)
        {
            Debug.Log("Nothing to drop");
            return;
        }

        Debug.Log("Dropping: " + heldObject.name);

        GrabbableObject grabbable = heldObject.GetComponent<GrabbableObject>();
        grabbable?.Release();

        if (joint != null)
            Destroy(joint);

        heldObject = null;

        if (playerController != null)
            playerController.isCarrying = false;

        Debug.Log("Drop successful");
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
