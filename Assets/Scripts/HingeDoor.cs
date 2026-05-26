using UnityEngine;

public class HingeDoor : MonoBehaviour
{
    public float openAngle = 90f;
    public float speed = 4f;

    [Header("Direction Fix")]
    public bool flipOpenDirection = false;

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

    public void OpenFromSide(bool playerInFront)
    {
        float direction = playerInFront ? -1f : 1f;

        if (flipOpenDirection)
            direction *= -1f;

        targetRotation = closedRotation * Quaternion.Euler(
            0,
            openAngle * direction,
            0
        );
    }

    public void CloseDoor()
    {
        targetRotation = closedRotation;
    }
}