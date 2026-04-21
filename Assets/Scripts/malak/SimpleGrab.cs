using UnityEngine;
using UnityEngine.InputSystem;
public class SimpleGrab : MonoBehaviour
{
    public Transform holdPoint;   // where object will sit
    public float grabRange = 2f;

    private GameObject heldObject;

    void Update()
    {
        // PRESS E to grab
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject == null)
            {
                TryGrab();
            }
            else
            {
                Drop();
            }
        }
    }

    void TryGrab()
    {
        Debug.Log("Trying to grab...");

        Collider[] hits = Physics.OverlapSphere(transform.position, grabRange);

        Debug.Log("Objects found in range: " + hits.Length);

        foreach (var hit in hits)
        {
            Debug.Log("Checking: " + hit.name);

            if (hit.CompareTag("Grabbable"))
            {
                Debug.Log("Grabbable object found: " + hit.name);

                heldObject = hit.gameObject;

                heldObject.GetComponent<Rigidbody>().isKinematic = true;

                heldObject.transform.position = holdPoint.position;
                heldObject.transform.parent = holdPoint;

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
            Debug.Log("Tried to drop but nothing is held");
            return;
        }

        Debug.Log("Dropping object: " + heldObject.name);

        heldObject.transform.parent = null;
        heldObject.GetComponent<Rigidbody>().isKinematic = false;
        heldObject = null;

        Debug.Log("Drop successful");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, grabRange);
    }
}
