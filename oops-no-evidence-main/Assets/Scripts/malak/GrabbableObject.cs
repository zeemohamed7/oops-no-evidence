using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    public bool isGrabbed = false;
    public GameObject currentHolder;

    public bool TryGrab(GameObject player)
    {
        // If already grabbed ? reject
        if (isGrabbed)
        {
            Debug.Log("Object already grabbed!");
            return false;
        }

        // Allow grab
        isGrabbed = true;
        currentHolder = player;

        Debug.Log(player.name + " grabbed the object");
        return true;
    }

    public void Release()
    {
        isGrabbed = false;
        currentHolder = null;

        Debug.Log("Object released");
    }
    
}
