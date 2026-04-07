using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public InputActionReference interactAction;
    public float reach = 3f;
    public Camera cam;

    private void Update()
    {
        if (interactAction.action.triggered) PerformRaycast();
    }

    private void OnEnable()
    {
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        interactAction.action.Disable();
    }

    private void PerformRaycast()
    {
        RaycastHit hit;
        // Shoot a line from the center of the screen
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, reach))
            // 1. Check for Hiding Spot
            if (hit.collider.TryGetComponent(out HidingSpot spot))
                spot.ToggleHide(gameObject);
        // 2. Check for Body (For later)
        /*
            if (hit.collider.TryGetComponent(out BodyDragger body))
            {
                body.StartDragging();
                return;
            }
            */
        // 3. Check for Doors, etc.
    }
}