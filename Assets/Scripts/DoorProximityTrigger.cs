using UnityEngine;
using System.Collections;

public class DoorProximityTrigger : MonoBehaviour
{
    public HingeDoor door;
    public HingeDoor leftDoor;
    public HingeDoor rightDoor;

    public float closeDelay = 1f;

    private int charactersInside = 0;
    private Coroutine closeRoutine;

    private bool IsAllowed(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Guard")  || other.CompareTag("Visitor");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAllowed(other)) return;

        charactersInside++;

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        Vector3 directionToCharacter = other.transform.position - transform.position;
        bool characterInFront = Vector3.Dot(transform.forward, directionToCharacter) > 0;

        if (door != null) door.OpenFromSide(characterInFront);
        if (leftDoor != null) leftDoor.OpenFromSide(characterInFront);
        if (rightDoor != null) rightDoor.OpenFromSide(characterInFront);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowed(other)) return;

        charactersInside--;

        if (charactersInside <= 0)
        {
            charactersInside = 0;
            closeRoutine = StartCoroutine(CloseAfterDelay());
        }
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);

        if (charactersInside == 0)
        {
            if (door != null) door.CloseDoor();
            if (leftDoor != null) leftDoor.CloseDoor();
            if (rightDoor != null) rightDoor.CloseDoor();
        }
    }
}