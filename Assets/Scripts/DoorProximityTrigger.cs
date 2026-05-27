using UnityEngine;

public class DoorProximityTrigger : MonoBehaviour
{
    public HingeDoor door;
    public HingeDoor leftDoor;
    public HingeDoor rightDoor;

    private int playersInside = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside++;

        Vector3 directionToPlayer = other.transform.position - transform.position;
        bool playerInFront = Vector3.Dot(transform.forward, directionToPlayer) > 0;

        if (door != null)
            door.OpenFromSide(playerInFront);

        if (leftDoor != null)
            leftDoor.OpenFromSide(playerInFront);

        if (rightDoor != null)
            rightDoor.OpenFromSide(playerInFront);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInside--;

        if (playersInside <= 0)
        {
            playersInside = 0;

            if (door != null)
                door.CloseDoor();

            if (leftDoor != null)
                leftDoor.CloseDoor();

            if (rightDoor != null)
                rightDoor.CloseDoor();
        }
    }
}