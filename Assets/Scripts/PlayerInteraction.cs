using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("When any tool is equipped the Interact key is used by the tool, not for hiding.")]
    public ToolInventory toolInventory;

    public float reach = 3f;

    // Track the components locally instead of using a global reference asset
    private PlayerInput playerInput;
    private InputAction localInteractAction;
    
    private SecurityTerminal activeTerminal;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();

        if (playerInput != null)
        {
            localInteractAction = playerInput.actions.FindAction("Interact");
        
            if (localInteractAction == null)
            {
                Debug.LogError($"[INTERACT ERROR] Could not find an action named 'Interact' inside the current Input Asset map! Double check your spelling.");
            }
            else
            {
                Debug.Log($"[INTERACT CONFIG] Successfully mapped local 'Interact' action for {gameObject.name}. Device count attached: {playerInput.devices.Count}");
            }
        }
        else
        {
            Debug.LogError($"[INTERACT ERROR] No PlayerInput component found on {gameObject.name} or its parents!");
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (localInteractAction == null) return;

        if (toolInventory != null && toolInventory.GetSelectedSlot() != -1) return;

        // Handle HOLD input for Terminals
        if (localInteractAction.IsPressed())
        {
            HandleTerminalHoldCheck();
        }
        else
        {
            // Reset terminal progress if the player lets go of the button
            if (activeTerminal != null)
            {
                activeTerminal.ResetProgress();
                activeTerminal = null;
            }
        }

        // Handle CLICK input for Hiding Spots
        if (localInteractAction.WasPressedThisFrame()) 
        {
            Debug.Log($"[INTERACT CLICK] {gameObject.name} physically pressed the Interact button! Running proximity check next...");
            PerformHidingCheck(); // Fixed to match the method name below
        }
    }
    
    private void HandleTerminalHoldCheck()
    {
        var mask = LayerMask.GetMask("Interactable");
        var hitColliders = Physics.OverlapSphere(transform.position, reach, mask);

        SecurityTerminal foundTerminal = null;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out SecurityTerminal terminal))
            {
                foundTerminal = terminal;
                break;
            }
        }

        // If we are near a terminal and holding the button, progress it
        if (foundTerminal != null)
        {
            activeTerminal = foundTerminal;
            activeTerminal.AddProgress(Time.deltaTime);
        }
        else if (activeTerminal != null)
        {
            // Player walked away while holding button
            activeTerminal.ResetProgress();
            activeTerminal = null;
        }
    }

    private void PerformHidingCheck()
    {
        var mask = LayerMask.GetMask("Interactable");
        var hitColliders = Physics.OverlapSphere(transform.position, reach, mask);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out HidingSpot spot))
            {
                spot.ToggleHide(gameObject);
                return;
            }
        }
    }
}