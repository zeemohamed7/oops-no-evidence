using UnityEngine;
using UnityEngine.InputSystem;

public enum ToolType { Mop, BodyBag, Blacklight }

public class ToolManager : MonoBehaviour
{
    public ToolType currentTool;

    private InputSystem_Actions input;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void OnEnable()
    {
        input.Player.Enable();

        input.Player.Next.performed += OnNextTool;
        input.Player.Previous.performed += OnPreviousTool;
    }

    void OnDisable()
    {
        input.Player.Next.performed -= OnNextTool;
        input.Player.Previous.performed -= OnPreviousTool;

        input.Player.Disable();
    }

    void Start()
    {
        currentTool = ToolType.Mop;
    }

    void OnNextTool(InputAction.CallbackContext context)
    {
        currentTool = (ToolType)(((int)currentTool + 1) % 3);
        Debug.Log("Current Tool: " + currentTool);
    }

    void OnPreviousTool(InputAction.CallbackContext context)
    {
        currentTool = (ToolType)(((int)currentTool - 1 + 3) % 3);
        Debug.Log("Current Tool: " + currentTool);
    }
}