using UnityEngine;

public class HingeDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float speed = 4f;

    private Quaternion closedRotation;
    private Quaternion targetRotation;

    void Start()
    {
        closedRotation = transform.rotation;
        targetRotation = closedRotation;
    }

    void Update()
    {
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * speed
        );
    }

    public void OpenByPlayerMovement(Vector3 playerMoveDirection)
    {
        Vector3 localMoveDir = transform.InverseTransformDirection(playerMoveDirection);

        float angle = localMoveDir.z > 0 ? -openAngle : openAngle;

        targetRotation = closedRotation * Quaternion.Euler(0, angle, 0);
    }

    public void CloseDoor()
    {
        targetRotation = closedRotation;
    }
}