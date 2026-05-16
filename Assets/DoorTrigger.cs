using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    public SlidingDoor slidingDoor; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (slidingDoor != null)
            {
                slidingDoor.OpenDoor();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if they stepped out of the zone
        if (other.CompareTag("Player"))
        {
            if (slidingDoor != null)
            {
                slidingDoor.CloseDoor();
            }
        }
    }
}