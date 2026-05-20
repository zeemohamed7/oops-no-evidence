using UnityEngine;
using UnityEngine.InputSystem;

public class FurnitureGrab : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference grabAction;

    [Header("Setup")]
    public Transform holdPoint;
    public float grabRange = 3f;
    public LayerMask furnitureLayer;

    [Header("Carry Offset")]
    public Vector3 localCarryPosition = Vector3.zero;
    public Vector3 localCarryRotation = Vector3.zero;

    private FurnitureItem heldFurniture;
    private Rigidbody heldRb;
    private Collider[] playerColliders;
    private Collider[] furnitureColliders;
    private PlayerAnimationDriver animationDriver;

    void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider>();
        animationDriver = GetComponent<PlayerAnimationDriver>();
    }

    void OnEnable()
    {
        if (grabAction != null)
            grabAction.action.Enable();
    }

    void OnDisable()
    {
        if (grabAction != null)
            grabAction.action.Disable();
    }

    void Update()
    {
        if (grabAction == null) return;

        if (grabAction.action.WasPressedThisFrame())
        {
            if (heldFurniture == null)
                TryGrabFurniture();
            else
                DropFurniture();
        }
    }

    void TryGrabFurniture()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            grabRange,
            furnitureLayer,
            QueryTriggerInteraction.Collide
        );

        FurnitureItem nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            FurnitureItem item = hit.GetComponentInParent<FurnitureItem>();
            FurnitureSnap snapState = item.GetComponent<FurnitureSnap>();
            if (snapState != null && snapState.IsSolved)
                continue;
            if (item == null) continue;

            float distance = Vector3.Distance(transform.position, item.transform.position);

            if (distance < nearestDistance)
            {
                nearest = item;
                nearestDistance = distance;
            }
        }

        if (nearest == null)
        {
            Debug.Log("No furniture in range");
            return;
        }

        heldFurniture = nearest;
        FurnitureSnap snap = heldFurniture.GetComponent<FurnitureSnap>();
        if (snap != null)
        {
            snap.EnterRearrangeMode();
        }
        heldRb = heldFurniture.GetComponent<Rigidbody>();

        if (heldRb == null)
        {
            Debug.LogWarning("Furniture needs Rigidbody: " + heldFurniture.name);
            heldFurniture = null;
            return;
        }

        furnitureColliders = heldFurniture.GetComponentsInChildren<Collider>();

        foreach (Collider playerCol in playerColliders)
        {
            foreach (Collider furnitureCol in furnitureColliders)
            {
                Physics.IgnoreCollision(playerCol, furnitureCol, true);
            }
        }

        heldRb.linearVelocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
        heldRb.useGravity = false;
        heldRb.isKinematic = true;

        heldFurniture.transform.SetParent(holdPoint);
        heldFurniture.transform.localPosition = localCarryPosition;
        heldFurniture.transform.localRotation = Quaternion.Euler(localCarryRotation);

        animationDriver.SetCarrying(true);

        Debug.Log("Furniture grabbed: " + heldFurniture.name);
    }

    void DropFurniture()
    {
        if (heldFurniture == null) return;

        FurnitureItem furnitureToDrop = heldFurniture;
        Rigidbody rbToDrop = heldRb;

        furnitureToDrop.transform.SetParent(null);

        if (rbToDrop != null)
        {
            rbToDrop.isKinematic = false;
            rbToDrop.useGravity = true;
            rbToDrop.linearVelocity = Vector3.zero;
            rbToDrop.angularVelocity = Vector3.zero;
        }

        if (playerColliders != null && furnitureColliders != null)
        {
            foreach (Collider playerCol in playerColliders)
            {
                foreach (Collider furnitureCol in furnitureColliders)
                {
                    Physics.IgnoreCollision(playerCol, furnitureCol, false);
                }
            }
        }

        FurnitureSnap snap = furnitureToDrop.GetComponent<FurnitureSnap>();

        if (snap != null)
            snap.TrySnap();

        animationDriver.SetCarrying(false);
        animationDriver?.PlayDrop();

        Debug.Log("Furniture dropped: " + furnitureToDrop.name);

        heldFurniture = null;
        heldRb = null;
        furnitureColliders = null;
    } 

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        if (holdPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(holdPoint.position, 0.15f);
        }
    }
}