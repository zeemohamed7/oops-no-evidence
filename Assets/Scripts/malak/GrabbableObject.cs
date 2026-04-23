using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    [Header("State")]
    public bool isGrabbed = false;
    public GameObject currentHolder;

    
    public bool TryGrab(GameObject player)
    {
        //Prevent multiple players grabbing at the same time
        if (isGrabbed)
        {
            Debug.Log("Object already grabbed by: " + currentHolder?.name);
            return false;
        }


        isGrabbed = true;
        currentHolder = player;

        Debug.Log(player.name + " grabbed " + gameObject.name);
        return true;
    }

    public void Release()
    {
        Debug.Log(gameObject.name + " released");

        isGrabbed = false;
        currentHolder = null;
    }
}