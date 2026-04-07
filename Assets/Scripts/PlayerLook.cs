using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Input")] public InputActionReference lookAction;

    [Header("Settings")] public float mouseSensitivity = 0.5f;

    [Header("References")] public Transform playerBody; // The main Player capsule

    private float xRotation;

    private void Start()
    {
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Read the mouse/stick movement
        var lookInput = lookAction.action.ReadValue<Vector2>();

        var mouseX = lookInput.x * mouseSensitivity;
        var mouseY = lookInput.y * mouseSensitivity;

        // Up and Down (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevents looking backwards through your own legs

        // Apply up/down rotation to the Camera (this object)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Apply left/right rotation to the entire Player Body
        playerBody.Rotate(Vector3.up * mouseX);
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }
}