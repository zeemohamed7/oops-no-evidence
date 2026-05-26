using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections.Generic;
[RequireComponent(typeof(AudioSource))]
// Using Finite State Machine (FSM) to make sure guard is only ever doing exactly one behavior at a time
public class GuardStateMachine : MonoBehaviour
{
    // TEMP FOR DEBUGGING - CHANGE TO PRIVATE
    private Animator anim;
    public enum State
    {
        Patrolling,
        Alerted,
        Chasing,
        Searching,
        Evicting,
        Suspicious
    }

    public float patrolSpeed = 2.5f;

    public float chaseSpeed = 5f;
    public float losePlayerTime = 3f;

    [Header("Grace Period")]
    public float postEvictionGrace = 3f; // 3 seconds of safety
    private float graceTimer;
    
    [Header("Global Suspicion Rates")] public float suspicionIncreaseRate = 25f;


    public UnityEvent OnPlayerDetected;

    public UnityEvent OnPlayerLost;

    public float searchDuration = 4f;
    public float searchTurnSpeed = 2f;
    public float searchAngle = 60f; // How far left/right they look

    public Color patrolColor = new(0, 0, 0, 0);
    public Color alertedColor = Color.yellow;
    public Color chasingColor = Color.red;
    public Color searchingColor = new(1f, 0.5f, 0f);

    [Header("Eviction Settings")]
    public string playerTag = "Player"; 
    public Transform kickOutPoint;      
    public float evictionDelay = 1.0f; 
    private float evictionTimer;
    private GameObject playerToEvict;
    
    [Header("Specific Barks")]
    public AudioSource audioSource;
    public AudioClip spotSound;    // The "Huh?"
    public AudioClip giveUpSound;  // The "Sigh"
    public AudioClip[] chaseBarks; // Chasing
    
    [Header("Footprint Suspicion")]
    public float footprintDetectionRadius = 3f;
    public float suspicionPerPrint = 15f;
    public float stopDuration = 2.0f;
    public AudioClip ewSound; // Assign an "Ew" sound here
    private float stopTimer;
    private HashSet<GameObject> reactedPrints = new HashSet<GameObject>(); // ensures they react once, wait 2 seconds, and then keep walking.
    
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
        patrol = GetComponent<GuardPatrol>(); 
        audioSource = GetComponent<AudioSource>();
        anim = GetComponentInChildren<Animator>();
        EnterPatrol();
    }

    private void Update()
    {
        if (graceTimer > 0) graceTimer -= Time.deltaTime;
        switch (currentState)
        {
            // States to make sure it's only doing one thing at a time
            case State.Patrolling: UpdatePatrol(); break;
            case State.Alerted: UpdateAlerted(); break;
            case State.Chasing: UpdateChasing(); break;
            case State.Searching: UpdateSearching(); break;
            case State.Evicting: UpdateEvicting(); break;
            case State.Suspicious: UpdateSuspicious(); break;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // If we catch the player and we aren't already evicting someone
        if (other.CompareTag(playerTag) && currentState != State.Evicting)
        {
            playerToEvict = other.gameObject;
            EnterEvicting();
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
    
    private void PlayRandomBark(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return; 
    
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Play();
    }

    // --- STATE 1: PATROLLING ---
    private void EnterPatrol()
    {
        CancelInvoke(); // Stop the guard from jumping into Chasing if he was just Alerted
        if(anim != null) anim.SetBool("isChasing", false);
        currentState = State.Patrolling;
        UpdateVisuals("", Color.white);
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        if (patrol != null) patrol.ResumePatrol();
    }

    private void UpdatePatrol()
    {
        if (visionCone.canSeePlayer && graceTimer <= 0)
        {
            EnterAlerted();
        }
        else
        {
            CheckForFootprints();
        }
    }
    
    private void CheckForFootprints()
    {
        // Look for footprints in a small circle around the guard
        Collider[] hits = Physics.OverlapSphere(transform.position, footprintDetectionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Footprint") && !reactedPrints.Contains(hit.gameObject))
            {
                EnterSuspicious(hit.gameObject);
                break;
            }
        }
    }

    // --- STATE 2: ALERTED ---
    private void EnterAlerted()
    {
        currentState = State.Alerted;
        UpdateVisuals("?", alertedColor);
        if (patrol != null) patrol.StopPatrol();
        agent.isStopped = true;

        if (spotSound != null) 
        {
            audioSource.clip = spotSound;
            audioSource.Play();
        }

        OnPlayerDetected.Invoke();
    
        Invoke(nameof(EnterChasing), 1.0f); 
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
        if(anim != null) anim.SetBool("isChasing", true);
        currentState = State.Chasing;
        UpdateVisuals("!", chasingColor);
        PlayRandomBark(chaseBarks);
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
            if (hit.collider.CompareTag(playerTag))
                hasLoS = true;

        Debug.DrawLine(eye, target, hasLoS ? Color.red : Color.green); // DEBUGGING


        // Stay "locked on" if you're visible and within range
        if (hasLoS && dist < visionCone.radius * 1.2f)
        {
            // PLAYER SEEN: Update destination to your current feet and reset timer
            agent.SetDestination(visionCone.playerRef.transform.position);
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                GameEvents.OnSuspicionAdded?.Invoke(suspicionIncreaseRate * Time.deltaTime);
            }
        }
        else
        {
            // LOST SIGHT OF PLAYER: Keep walking to the last place I saw you

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
    // --- STATE 55: EVICTING ---

    private void EnterEvicting()
    {
        CancelInvoke();
        currentState = State.Evicting;
        UpdateVisuals("GOTCHA!", Color.red);
    
        agent.isStopped = false;
        agent.speed = patrolSpeed; 
        agent.SetDestination(kickOutPoint.position);

        // 2. Disable the player so they can't fight back
        if (playerToEvict != null)
        {
            var controller = playerToEvict.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            
            var playerScript = playerToEvict.GetComponent<TopDownPlayerController>();
            if (playerScript != null) playerScript.enabled = false;
        }
        

    }

    private void UpdateEvicting()
    {
        if (playerToEvict == null) { EnterPatrol(); return; }

        // 1. Keep the player "stuck" to the guard's front
        Vector3 holdPosition = transform.position + transform.forward * 0.6f; 
        playerToEvict.transform.position = holdPosition;
    
        // Make the player look the same way as the guard
        playerToEvict.transform.rotation = transform.rotation;

        // 2. Check if we arrived at the Eviction Point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            ReleasePlayer();
        }
    }
    
    private void ReleasePlayer()
    {
        if (playerToEvict != null)
        {
            var controller = playerToEvict.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = true; 

            var playerScript = playerToEvict.GetComponent<TopDownPlayerController>();
            if (playerScript != null) playerScript.enabled = true;
        }

        graceTimer = postEvictionGrace; 
        playerToEvict = null;
        EnterPatrol();
    }
    
    // --- STATE 6: SUSPICIOUS ---
    
    private void EnterSuspicious(GameObject footprint)
    {
        currentState = State.Suspicious;
        UpdateVisuals("EW!", alertedColor);
        
        agent.isStopped = true;
        stopTimer = stopDuration;

        // Play "Ew" Sound
        if (ewSound != null)
        {
            audioSource.PlayOneShot(ewSound);
        }

        // Increase Global Suspicion
        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            GameEvents.OnSuspicionAdded?.Invoke(suspicionIncreaseRate * Time.deltaTime);
        }

        // Prevent reacting to this exact print again for 10 seconds
        StartCoroutine(IgnorePrintTemporary(footprint));
        
    }

    private void UpdateSuspicious()
    {
        stopTimer -= Time.deltaTime;

        // If the guard sees the player while looking at a footprint, stop being "disgusted" and start chasing!
        if (visionCone.canSeePlayer && graceTimer <= 0)
        {
            EnterAlerted();
            return;
        }

        if (stopTimer <= 0)
        {
            EnterPatrol();
        }
    }

    private IEnumerator IgnorePrintTemporary(GameObject print)
    {
        reactedPrints.Add(print);
        yield return new WaitForSeconds(10f); // How long before he reacts to this spot again
        reactedPrints.Remove(print);
    }
    
    private void GiveUpChase()
    {
        searchTimer = 0f;
        if (giveUpSound != null) 
        {
            audioSource.clip = giveUpSound;
            audioSource.Play();
        }
        OnPlayerLost.Invoke();
        EnterPatrol();
    }
}