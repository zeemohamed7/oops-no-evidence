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
    public float grabCooldown = 0.25f;


    private FurnitureItem heldFurniture;
    private Rigidbody heldRb;
    private Collider[] playerColliders;
    private Collider[] furnitureColliders;
    private PlayerAnimationDriver animationDriver;
    private TopDownPlayerController playerController;
    private ToolInventory toolInventory;
    private float nextGrabTime = 0f;

    void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider>();
        animationDriver = GetComponent<PlayerAnimationDriver>();
        playerController = GetComponent<TopDownPlayerController>();
        toolInventory = GetComponent<ToolInventory>();
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
        if (Time.time < nextGrabTime) return;

        if (grabAction.action.WasPressedThisFrame())
        {
            if (heldFurniture != null)
            {
                DropFurniture();
                nextGrabTime = Time.time + grabCooldown;
                return;
            }

            if (toolInventory != null && toolInventory.GetSelectedSlot() != -1)
            {
                Debug.Log("Cannot grab furniture while holding a tool.");
                return;
            }

            TryGrabFurniture();
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
            if (item == null) continue;

            FurnitureSnap snapState = item.GetComponent<FurnitureSnap>();
            if (snapState != null && snapState.IsSolved)
                continue;

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

        if (playerController != null)
        playerController.isCarrying = true;

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

        if (playerController != null)
        playerController.isCarrying = false;
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