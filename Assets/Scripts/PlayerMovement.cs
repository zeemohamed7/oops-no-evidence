using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")] public float walkSpeed = 4f;

    public float sprintSpeed = 7f;
    public float crouchSpeed = 2f;

    [Header("Jump & Gravity")] public float jumpHeight = 1.2f;

    public float gravity = -15f; // Slightly higher gravity feels less "floaty"

    [Header("Crouch Settings")] public float standingHeight = 2f;

    public float crouchingHeight = 1f;

    [Header("Input Actions")] public InputActionReference moveAction;

    public InputActionReference jumpAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;

    private CharacterController controller;
    private bool isCrouching;
    private bool isGrounded;
    private Vector3 velocity;

    private void Awake()
    {
        // Automatically grab the Character Controller attached to the Player
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. Ground Check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f; // Keeps the player snapped to the floor

        // 2. Determine Speed & Height (Stealth Logic)
        var currentSpeed = walkSpeed;

        if (crouchAction.action.IsPressed())
        {
            // Shrink the player and slow them down
            controller.height = crouchingHeight;
            currentSpeed = crouchSpeed;
            isCrouching = true;
        }
        else
        {
            // Stand up
            controller.height = standingHeight;
            isCrouching = false;

            // Only allow sprinting if we are NOT crouching
            if (sprintAction.action.IsPressed()) currentSpeed = sprintSpeed;
        }

        // 3. Movement
        var inputDir = moveAction.action.ReadValue<Vector2>();

        // Move relative to where the player is currently facing
        var move = transform.right * inputDir.x + transform.forward * inputDir.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 4. Jumping
        // triggered means it was pressed exactly on this frame
        if (jumpAction.action.triggered && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // 5. Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // You MUST enable and disable Input Actions for the New Input System to work
    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
    }
}