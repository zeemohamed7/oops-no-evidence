using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TopDownPlayerController : MonoBehaviour
{
    private PlayerAnimationDriver animationDriver;

    [Header("Setup")]
    public Camera playerCamera;

    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;

    [Header("Crouch Settings")]
    public float standingHeight = 1.4f;
    public float crouchingHeight = 0.8f;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;

    [Header("Weight Penalty")]
    public bool isCarrying = false;

    [Range(0.1f, 1f)]
    public float carryMultiplier = 0.5f;

    private CharacterController controller;

    private void Awake()
    {
        animationDriver = GetComponent<PlayerAnimationDriver>();
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        DynamicCamera.Instance?.RegisterPlayer(transform);
    }

    private void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby")
            return;

        Vector3 moveDirection = HandleMovement();
        HandleMovementRotation(moveDirection);
    }

    private void OnEnable()
    {
        moveAction?.action.Enable();
        sprintAction?.action.Enable();
        crouchAction?.action.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        sprintAction?.action.Disable();
        crouchAction?.action.Disable();
    }

    private Vector3 HandleMovement()
    {
        float currentSpeed = walkSpeed;

        if (isCarrying)
            currentSpeed *= carryMultiplier;

        // --- HEIGHT & CENTER FIX ---
        if (crouchAction != null && crouchAction.action.IsPressed())
        {
            controller.height = crouchingHeight;
            currentSpeed = crouchSpeed;
        }
        else
        {
            controller.height = standingHeight;

            if (sprintAction != null && sprintAction.action.IsPressed())
                currentSpeed = sprintSpeed;
        }

        controller.center = new Vector3(0, controller.height / 2f, 0);
        // ----------------------------

        Vector2 input = moveAction != null
            ? moveAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * input.y) + (right * input.x);

        bool isWalking = moveDirection.sqrMagnitude > 0.01f;
        animationDriver?.SetWalking(isWalking);

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    
        // Increased gravity to keep her snappy on the pavement
        controller.Move(Vector3.down * 30f * Time.deltaTime);

        return moveDirection;
    }

    private void HandleMovementRotation(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * Time.deltaTime);
        }
    }
}