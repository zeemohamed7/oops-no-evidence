using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    public Vector3 openOffset = new Vector3(1.5f, 0f, 0f); // How far it slides out
    public float speed = 3f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Vector3 targetPosition;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
        targetPosition = closedPosition;
    }

    void Update()
    {
        // Smoothly slide toward the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speed);
    }

    // Call these from a Trigger Zone
    public void OpenDoor() => targetPosition = openPosition;
    public void CloseDoor() => targetPosition = closedPosition;
}