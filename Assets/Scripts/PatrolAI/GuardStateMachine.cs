using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// Using Finite State Machine (FSM) to make sure guard is only ever doing exactly one behavior at a time
public class GuardStateMachine : MonoBehaviour
{
    // TEMP FOR DEBUGGING - CHANGE TO PRIVATE

    public enum State
    {
        Patrolling,
        Alerted,
        Chasing,
        Searching
    }

    public float patrolSpeed = 2.5f;

    public float chaseSpeed = 5f;
    public float losePlayerTime = 3f;

    [Header("Global Suspicion Rates")] public float suspicionIncreaseRate = 25f;

    public float suspicionDrainRate = 10f;

    public UnityEvent OnPlayerDetected;

    public UnityEvent OnPlayerLost;

    public float searchDuration = 4f;
    public float searchTurnSpeed = 2f;
    public float searchAngle = 60f; // How far left/right they look

    public Color patrolColor = new(0, 0, 0, 0);
    public Color alertedColor = Color.yellow;
    public Color chasingColor = Color.red;
    public Color searchingColor = new(1f, 0.5f, 0f);


    // TEMP FOR DEBUGGING - CHANGE TO PRIVATE

    public State currentState;


    [Header("Visual Feedback")] public TextMeshProUGUI alertText;

    private NavMeshAgent agent;
    private GuardPatrol patrol;
    private Quaternion searchStartRotation;
    private float searchTimer;
    private VisionCone visionCone;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        visionCone = GetComponent<VisionCone>();
        patrol = GetComponent<GuardPatrol>(); // Automatically looks for GuardPatrol.cs

        EnterPatrol();
    }

    private void Update()
    {
        switch (currentState)
        {
            // States to make sure it's only doing one thing at a time
            case State.Patrolling: UpdatePatrol(); break;
            case State.Alerted: UpdateAlerted(); break;
            case State.Chasing: UpdateChasing(); break;
            case State.Searching: UpdateSearching(); break;
        }
    }

    private void UpdateVisuals(string text, Color color)
    {
        if (alertText == null) return;

        alertText.text = text;
        alertText.color = color;

        // Hide the text entirely if it's empty
        alertText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    // --- STATE 1: PATROLLING ---
    private void EnterPatrol()
    {
        currentState = State.Patrolling;
        UpdateVisuals("", Color.white);
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        if (patrol != null) patrol.ResumePatrol();
    }

    private void UpdatePatrol()
    {
        // Player is seen, freak out!!
        if (visionCone.canSeePlayer)
        {
            EnterAlerted();
        }
        else
        {
            // Cool down if patrolling again
            if (SuspicionMeter.Instance != null && SuspicionMeter.Instance.globalSuspicion > 0)
                SuspicionMeter.Instance.ModifySuspicion(-suspicionDrainRate * Time.deltaTime);
        }
    }

    // --- STATE 2: ALERTED ---
    private void EnterAlerted()
    {
        currentState = State.Alerted;
        UpdateVisuals("?", alertedColor);
        if (patrol != null) patrol.StopPatrol();
        agent.isStopped = true;

        OnPlayerDetected.Invoke();
        Invoke(nameof(EnterChasing), 0.6f); // Wait 0.6 seconds in shock before running
    }

    private void UpdateAlerted()
    {
        if (visionCone.playerRef == null) return;

        var direction = (visionCone.playerRef.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
            transform.rotation =
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
    }

    // --- STATE 3: CHASING ---
    private void EnterChasing()
    {
        currentState = State.Chasing;
        UpdateVisuals("!", chasingColor);
        agent.isStopped = false;
        agent.speed = chaseSpeed;
    }

    private void UpdateChasing()
    {
        if (visionCone.playerRef == null) return;


        // Ignore cone vision (FOV) and Check Line of Sight (LoS) once to decide what to do
        var eye = transform.position + Vector3.up * 0.8f;
        var target = visionCone.playerRef.transform.position + Vector3.up * 0.5f;
        var dist = Vector3.Distance(eye, target);

        // If you hit a wall, player or closet, stop and say what you hit
        var combinedMask = visionCone.obstructionMask | LayerMask.GetMask("Target") | LayerMask.GetMask("Interactable");

        RaycastHit hit;
        var hasLoS = false;

        if (Physics.Raycast(eye, (target - eye).normalized, out hit, dist + 0.5f, combinedMask))
            // If the laser hit Player, hasLoS is true otherwise if it's hit obstruction or closet, LoS stays false
            if (hit.collider.CompareTag("Player"))
                hasLoS = true;

        Debug.DrawLine(eye, target, hasLoS ? Color.red : Color.green); // DEBUGGING


        // Stay "locked on" if you're visible and within range
        if (hasLoS && dist < visionCone.radius * 1.2f)
        {
            // PLAYER SEEN: Update destination to your current feet and reset timer
            agent.SetDestination(visionCone.playerRef.transform.position);
            SuspicionMeter.Instance?.ModifySuspicion(suspicionIncreaseRate * Time.deltaTime);
        }
        else
        {
            // LOST SIGHT OF PLAYER: Keep walking to the last place I saw you
            SuspicionMeter.Instance?.ModifySuspicion(-suspicionDrainRate * Time.deltaTime);

            // Check if we've arrived at the last spot or got stuck on a wall
            var reachedSpot = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
            var isStuck = agent.velocity.sqrMagnitude < 0.1f;


            // If last seen spot is reached, enter searching
            if (reachedSpot || isStuck) EnterSearching();
        }
    }

    // --- STATE 4: SEARCHING ---

    private void EnterSearching()
    {
        currentState = State.Searching;
        UpdateVisuals("?", searchingColor);
        agent.isStopped = true; // Stop walking
        searchTimer = 0f;
        searchStartRotation = transform.rotation; // Remember which way we were facing
    }

    private void UpdateSearching()
    {
        searchTimer += Time.deltaTime;

        // 1. If player SEEN, go back to chasing
        if (visionCone.canSeePlayer)
        {
            EnterChasing();
            return;
        }

        // 2. Scan
        // Sine wave to oscillate the rotation left and right
        var angle = Mathf.Sin(Time.time * searchTurnSpeed) * searchAngle;
        transform.rotation = searchStartRotation * Quaternion.Euler(0, angle, 0);

        // 3. Time's up, give up
        if (searchTimer >= searchDuration) GiveUpChase();
    }


    private void GiveUpChase()
    {
        searchTimer = 0f;
        OnPlayerLost.Invoke();
        EnterPatrol();
    }
}