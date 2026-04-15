using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Setup")] public Camera playerCamera;

    [Header("Movement Speeds")] public float walkSpeed = 5f;

    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;

    [Header("Crouch Settings")] public float standingHeight = 2f;

    public float crouchingHeight = 1f;

    [Header("Input Actions")] public InputActionReference moveAction;

    public InputActionReference sprintAction;
    public InputActionReference crouchAction;

    private CharacterController controller;

    //below code added by malak
    //this is to catch if the player is holding something if yes its movements will be slow by 50%

    [Header("Weight Penalty")]
    public bool isCarrying = false;
    [Range(0.1f, 1f)]
    public float carryMultiplier = 0.5f;



    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        // Safety checks without the log spam
        if (playerCamera == null || Mouse.current == null) return;

        HandleMovement();
        HandleRotation();

        //just test remove it later when the grap model found!
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            isCarrying = !isCarrying;
            Debug.Log("Carrying: " + isCarrying);
        }

    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
        crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
        crouchAction.action.Disable();
    }

    private void HandleMovement()
    {

        // 1. Determine Speed & Height
        var currentSpeed = walkSpeed;

        // Apply weight penalty FIRST
        if (isCarrying)
        {
            currentSpeed *= carryMultiplier;
        }

        if (crouchAction.action.IsPressed())
        {
            controller.height = crouchingHeight;
            currentSpeed = crouchSpeed;
        }
        else
        {
            controller.height = standingHeight;
            if (sprintAction.action.IsPressed()) currentSpeed = sprintSpeed;
        }

        // 2. Read Input
        var input = moveAction.action.ReadValue<Vector2>();

        // 3. Move based on WORLD directions
        var moveDirection = new Vector3(input.x, 0, input.y);
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 4. Gravity (Constant downward force)
        controller.Move(Vector3.down * 20f * Time.deltaTime);

        //DEBUG
        if (moveAction == null)
        {
            Debug.LogError("Move Action is NULL!");
        }

        if (sprintAction == null)
        {
            Debug.LogError("Sprint Action is NULL!");
        }

        if (crouchAction == null)
        {
            Debug.LogError("Crouch Action is NULL!");
        }
    }

    private void HandleRotation()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var ray = playerCamera.ScreenPointToRay(mousePos);
        var groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out var rayDistance))
        {
            var targetPoint = ray.GetPoint(rayDistance);
            var lookDir = targetPoint - transform.position;
            lookDir.y = 0;

            if (lookDir.magnitude > 0.1f) transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}