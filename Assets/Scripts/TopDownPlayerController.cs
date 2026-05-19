using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class TopDownPlayerController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerAnimationDriver animationDriver;
    private PlayerInput playerInput;
    private Animator animator;

    [Header("Setup")]
    public Camera playerCamera;

    [Header("Movement Speeds")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;

    [Header("Crouch Settings")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;

    [Header("Carry")]
    public bool isCarrying = false;

    [Range(0.1f, 1f)]
    public float carryMultiplier = 0.5f;

    //malak
    [Header("Footsteps")]
    public AudioSource footstepSource;

    // INPUT VALUES
    private Vector2 moveInput;
    private bool sprintHeld;
    private bool crouchHeld;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animationDriver = GetComponent<PlayerAnimationDriver>();
        playerInput = GetComponent<PlayerInput>();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        foreach (var map in playerInput.actions.actionMaps)
        {
            Debug.Log("MAP: " + map.name);

            foreach (var action in map.actions)
            {
                Debug.Log(" - ACTION: " + action.name);
            }
        }
        Debug.Log("PLAYER CONTROLLER STARTED");

        if (playerInput != null)
        {
            Debug.Log("CURRENT MAP: " + playerInput.currentActionMap.name);
            Debug.Log("CONTROL SCHEME: " + playerInput.currentControlScheme);
        }

        DynamicCamera.Instance?.RegisterPlayer(transform);
    }

    private void Update()
    {
        
        // Ignore movement in lobby
        if (UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name == "Lobby")
            return;

        HandleMovement();
    }

    // ─────────────────────────────────────────────
    // SEND MESSAGES INPUT CALLBACKS
    // ─────────────────────────────────────────────

    // Matches the "Move" action in your Input Action Asset
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        
    }
    // Matches the "Sprint" action
    public void OnSprint(InputValue value)
    {
        sprintHeld = value.isPressed;
    }

    // Matches the "Crouch" action
    public void OnCrouch(InputValue value)
    {
        crouchHeld = value.isPressed;
    }
    

    // ─────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────

private void HandleMovement()
    {
        if (playerInput != null && !playerInput.enabled)
        {
            // Reset our internal tracking so we don't slide
            moveInput = Vector2.zero;
            
            // Send a false signal to the animator so it knows we aren't walking
            animationDriver?.SetWalking(false);

            //malak
            HandleFootsteps(false);

            return; 
        }
        
        float speed = walkSpeed;

        // Apply the carry multiplier to our base walking speed
        if (isCarrying)
            speed *= carryMultiplier;

        if (crouchHeld)
        {
            controller.height = crouchingHeight;
            speed = crouchSpeed;
        }
        else
        {
            controller.height = standingHeight;

            //Only allow the sprint speed upgrade if the player IS NOT carrying an object
            if (sprintHeld && !isCarrying)
            {
                speed = sprintSpeed;
            }
        }

        controller.center = new Vector3(0, controller.height / 2f, 0);

        // --- THE CRITICAL FIX START ---
        // 1. Get the camera's forward and right vectors
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        // 2. "Flatten" them so the player doesn't walk into the ground 
        // because the camera is tilted down
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y) + (right * moveInput.x);

        // IMMOBILIZATION WINDOW CHECK
        // Look at Layer 0 of our Animator. If the current active state name is 
        // string matched to "lift body", zero out speed vectors to lock positions.
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("lift body"))
        {
            moveDirection = Vector3.zero;
        }

        bool isWalking = moveDirection.sqrMagnitude > 0.01f;
        animationDriver?.SetWalking(isWalking);

        //malak
        HandleFootsteps(isWalking);

        controller.Move(moveDirection * speed * Time.deltaTime);
        controller.Move(Vector3.down * 30f * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * Time.deltaTime);
        }
    }

    private void HandleFootsteps(bool isWalking)
    {
        if (footstepSource == null)
            return;

        if (isWalking)
        {
            if (!footstepSource.isPlaying)
            {
                footstepSource.Play();
            }
        }
        else
        {
            if (footstepSource.isPlaying)
            {
                footstepSource.Stop();
            }
        }
    }
}