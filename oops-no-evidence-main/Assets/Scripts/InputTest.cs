using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    PlayerControls controls;
    Vector2 moveInput;
    Vector2 lastInput;

    void Awake()
    {
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    void Update()
    {
        Vector2 currentInput = controls.Player.Move.ReadValue<Vector2>();

        if (currentInput != lastInput)
        {
            Debug.Log("Move Input: " + currentInput);
            lastInput = currentInput;
        }
    }
}