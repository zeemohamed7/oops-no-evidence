using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Tooltip("When any tool is equipped the Interact key is used by the tool, not for hiding.")]
    public ToolInventory toolInventory;

    public float reach = 3f;

    // 🟢 CHANGE 1: Track the components locally instead of using a global reference asset
    private PlayerInput playerInput;
    private InputAction localInteractAction;

    private void Start()
    {
        playerInput = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();

        if (playerInput != null)
        {
            // 🔍 DIAGNOSTIC 1: See if the Action can actually be found by this string name
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

        // 🔍 DIAGNOSTIC 2: See if the physical button press is registering at all
        if (localInteractAction.WasPressedThisFrame()) 
        {
            Debug.Log($"[INTERACT CLICK] {gameObject.name} physically pressed the Interact button! Running proximity check next...");
            PerformProximityCheck();
        }
    }

    // 🟢 CHANGE 4: Clean up OnEnable/OnDisable. 
    // Since GameManager now manages waking up the action maps globally, 
    // we don't want individual player instances turning entire global maps on and off randomly!
    private void OnEnable() { }
    private void OnDisable() { }

    private void PerformProximityCheck()
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