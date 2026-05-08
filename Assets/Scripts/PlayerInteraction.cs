using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Input Setup")] public InputActionReference interactAction;

    [Tooltip("When any tool is equipped the Interact key is used by the tool, not for hiding.")]
    public ToolInventory toolInventory;

    public float reach = 3f;

    private void Update()
    {
        if (interactAction == null) return;

        // Don't try to hide when the player has a tool equipped — the tool uses E/Y instead.
        if (toolInventory != null && toolInventory.GetSelectedSlot() != -1) return;

        if (interactAction.action.WasPressedThisFrame()) PerformProximityCheck();
    }

    private void OnEnable()
    {
        // 1. Enable the specific action
        interactAction?.action.Enable();

        // 2. Optimal: Enable the entire Action Map (e.g., the "Player" map)
        // This prevents the key from being ignored if the map is asleep
        interactAction?.action.actionMap.Enable();
    }

    private void OnDisable()
    {
        interactAction?.action.Disable();
    }

    private void PerformProximityCheck()
    {
        var mask = LayerMask.GetMask("Interactable");
        var hitColliders = Physics.OverlapSphere(transform.position, reach, mask);

        foreach (var hitCollider in hitColliders)
            if (hitCollider.TryGetComponent(out HidingSpot spot))
            {
                spot.ToggleHide(gameObject);
                return;
            }
    }
}