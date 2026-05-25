using UnityEngine;

public class DoorProximityTrigger : MonoBehaviour
{
    [Header("Single Door")]
    public HingeDoor door;

    [Header("Double Doors")]
    public HingeDoor leftDoor;
    public HingeDoor rightDoor;

    private int playersInside = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInside++;

        Vector3 directionToPlayer = other.transform.position - transform.position;
        float side = Vector3.Dot(transform.forward, directionToPlayer);

        bool playerInFront = side > 0f;

        if (door != null)
            door.OpenFromSide(playerInFront);

        if (leftDoor != null)
            leftDoor.OpenFromSide(playerInFront);

        if (rightDoor != null)
            rightDoor.OpenFromSide(playerInFront);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInside--;

        if (playersInside > 0)
            return;

        if (door != null)
            door.CloseDoor();

        if (leftDoor != null)
            leftDoor.CloseDoor();

        if (rightDoor != null)
            rightDoor.CloseDoor();
    }
}