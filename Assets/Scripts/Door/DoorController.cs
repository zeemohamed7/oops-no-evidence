using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Settings")]
    public Transform hinge;        // Drag the Door_Hinge here
    public float openAngle = 90f;
    public float closeSpeed = 5f;

    private Quaternion targetRotation;
    private Quaternion closedRotation;
    private bool isOpen = false;

    private void Start()
    {
        if (hinge == null) hinge = transform.GetChild(0);
        
        // Save the starting rotation as "Home"
        closedRotation = hinge.localRotation;
        targetRotation = closedRotation;
    }

    private void Update()
    {
        // Smoothly rotate the hinge toward the target every frame
        hinge.localRotation = Quaternion.Slerp(hinge.localRotation, targetRotation, Time.deltaTime * closeSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Bidirectional Logic
            Vector3 dirToPlayer = other.transform.position - transform.position;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);

            // If dot > 0, player is in front, swing away (+90)
            // If dot < 0, player is behind, swing inward (-90)
            float angle = dot >= 0 ? openAngle : -openAngle;
            
            targetRotation = Quaternion.Euler(0, angle, 0);
            isOpen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Simply tell the door to return to its original "Home" rotation
            targetRotation = closedRotation;
            isOpen = false;
        }
    }
}