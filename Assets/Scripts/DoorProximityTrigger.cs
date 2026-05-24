using UnityEngine;

public class DoorProximityTrigger : MonoBehaviour
{
    public HingeDoor door;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 moveDirection = other.transform.forward;
            door.OpenByPlayerMovement(moveDirection);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            door.CloseDoor();
        }
    }
}