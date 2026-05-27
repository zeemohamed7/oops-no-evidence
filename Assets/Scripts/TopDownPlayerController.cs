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
    private float currentCarryPenalty = 0f; // 0 means no penalty active

    [Range(0.1f, 1f)]
    public float carryMultiplier = 0.5f;

    [Header("Noise / Suspicion")]
    [Tooltip("Sus added per second while sprinting.")]
    public float sprintSusPerSecond = 5f;
    [Tooltip("Player must be moving at least this fast (units/sec) to count as sprinting for sus.")]
    public float sprintSusSpeedThreshold = 7f;

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
        // 🟢 MULTIPLAYER MATCHING FIX: Force this instance to use its assigned control scheme
        if (playerInput != null && LobbyManager.Instance != null)
        {
            int myDeviceId = playerInput.devices.Count > 0 ? playerInput.devices[0].deviceId : -1;
            Debug.Log($"[START] My physical device ID is: {myDeviceId}");
        }

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
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Lobby")
            return;

        HandleMovement();
    }

    // ─────────────────────────────────────────────
    // SEND MESSAGES INPUT CALLBACKS
    // ─────────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        sprintHeld = value.isPressed;
    }

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
            moveInput = Vector2.zero;
            animationDriver?.SetWalking(false);
            HandleFootsteps(false);
            return; 
        }
        
        float speed = walkSpeed;

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
            if (sprintHeld && !isCarrying)
            {
                speed = sprintSpeed;
            }
        }

        controller.center = new Vector3(0, controller.height / 2f, 0);

        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y) + (right * moveInput.x);

        // ─────────────────────────────────────────────────────────────────────────
        // 🟢 CO-OP SMOOTH TETHER ZONE: Dampens movement linearly to kill spazzing
        // ─────────────────────────────────────────────────────────────────────────
        if (isCarrying && moveDirection.sqrMagnitude > 0.01f)
        {
            Grab localGrabScript = GetComponent<Grab>();
            if (localGrabScript != null && localGrabScript.GetHeldObject() != null)
            {
                GameObject body = localGrabScript.GetHeldObject();
                DeadbodyCarry carryScript = body.GetComponentInParent<DeadbodyCarry>();
                
                if (carryScript != null && carryScript.GetCarrierCount() > 1)
                {
                    GameObject otherPlayer = carryScript.GetOtherPlayer(gameObject);
                    if (otherPlayer != null)
                    {
                        float distanceBetweenPlayers = Vector3.Distance(transform.position, otherPlayer.transform.position);
                        
                        float minSlowingDistance = 1.3f; // 🟢 Point where elastic resistance begins
                        float maxSeparation = 1.8f;      // 🟢 Concrete stopping boundary 

                        if (distanceBetweenPlayers >= minSlowingDistance)
                        {
                            Vector3 dirToPartner = (otherPlayer.transform.position - transform.position).normalized;
                            float movementDot = Vector3.Dot(moveDirection.normalized, dirToPartner);

                            // Only penalize speed if walking AWAY from your co-op partner
                            if (movementDot < 0)
                            {
                                if (distanceBetweenPlayers >= maxSeparation)
                                {
                                    // 🛑 HARD STOP BOUNDARY REACHED
                                    moveDirection = Vector3.zero; 
                                }
                                else
                                {
                                    // 🟢 SLOW DOWN SMOOTHLY: Calculate a 1.0 to 0.0 modifier multiplier
                                    float t = (distanceBetweenPlayers - minSlowingDistance) / (maxSeparation - minSlowingDistance);
                                    float smoothMultiplier = Mathf.Lerp(1f, 0f, t);
                                    
                                    speed *= smoothMultiplier;
                                }
                            }
                        }
                    }
                }
            }
        }
        // ─────────────────────────────────────────────────────────────────────────

        // IMMOBILIZATION WINDOW CHECK
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("lift body"))
        {
            moveDirection = Vector3.zero;
        }

        bool isWalking = moveDirection.sqrMagnitude > 0.01f;
        animationDriver?.SetWalking(isWalking);

        HandleFootsteps(isWalking);

        // Sprint noise — only while actually moving fast enough
        if (isWalking && speed >= sprintSusSpeedThreshold
            && GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            GameEvents.OnSuspicionAdded?.Invoke(sprintSusPerSecond * Time.deltaTime);
        }

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
    
    // ─────────────────────────────────────────────
    // WEIGHT PENALTY FOR BODY
    // ─────────────────────────────────────────────

    public void SetCarryWeight(float dynamicMultiplier)
    {
        isCarrying = true;
        carryMultiplier = dynamicMultiplier;
    }

    public void ClearCarryPenalty()
    {
        isCarrying = false;
        carryMultiplier = 0.5f; 
    }
}