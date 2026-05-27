using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class GuardStateMachine : MonoBehaviour
{
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

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 5f;
    public float losePlayerTime = 3f;

    [Header("Grace Period")]
    public float postEvictionGrace = 3f; 
    private float graceTimer;
    
    [Header("Global Suspicion Rates")] 
    [Tooltip("Flat suspicion added instantly when stepping on a dirty footprint")]
    public float footprintSuspicionPenalty = 15f; 
    [Tooltip("How many suspicion points accumulate per second while being chased")]
    public float suspicionBuildSpeed = 20f;

    [Header("Events")]
    public UnityEvent OnPlayerDetected;
    public UnityEvent OnPlayerLost;

    [Header("Searching State Rules")]
    public float searchDuration = 4f;
    public float searchTurnSpeed = 2f;
    public float searchAngle = 60f; 

    [Header("State Colors")]
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
    
    [Header("Audio Barks")]
    public AudioSource audioSource;
    public AudioClip spotSound;    
    public AudioClip giveUpSound;  
    public AudioClip[] chaseBarks; 
    
    [Header("Footprint Suspicion")]
    public float footprintDetectionRadius = 3f;
    public float stopDuration = 2.0f;
    public AudioClip ewSound; 
    private float stopTimer;
    private HashSet<GameObject> reactedPrints = new HashSet<GameObject>(); 
    
    [Header("Status & Debugging")]
    public State currentState;

    [Header("Visual Feedback")] 
    public TextMeshProUGUI alertText;

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
            case State.Patrolling: UpdatePatrol(); break;
            case State.Alerted:    UpdateAlerted(); break;
            case State.Chasing:    UpdateChasing(); break;
            case State.Searching:  UpdateSearching(); break;
            case State.Evicting:   UpdateEvicting(); break;
            case State.Suspicious: UpdateSuspicious(); break;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
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
        alertText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
    
    private void PlayRandomBark(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return; 
        audioSource.clip = clips[Random.Range(0, clips.Length)];
        audioSource.Play();
    }

    // ─── STATE 1: PATROLLING ─────────────────────────────────────────────
    
    private void EnterPatrol()
    {
        CancelInvoke(); 
        if (anim != null) anim.SetBool("IsRunning", false);
        if (anim != null) anim.SetBool("IsWalking", true); 
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

    // ─── STATE 2: ALERTED ────────────────────────────────────────────────
    
    private void EnterAlerted()
    {
        if (visionCone.lastSpottedTarget != null)
            visionCone.playerRef = visionCone.lastSpottedTarget;
            
        currentState = State.Alerted;
        UpdateVisuals("?", alertedColor);
        if (patrol != null) patrol.StopPatrol();
        agent.isStopped = true;
        if (anim != null) anim.SetBool("IsWalking", false); 

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
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
    }

    // ─── STATE 3: CHASING ────────────────────────────────────────────────
    
    private void EnterChasing()
    {
        if (anim != null) anim.SetBool("IsRunning", true);
        if (anim != null) anim.SetBool("IsWalking", false);
        if (anim != null) anim.SetBool("LookAround", false); 
        currentState = State.Chasing;
        UpdateVisuals("!", chasingColor);
        PlayRandomBark(chaseBarks);
        agent.isStopped = false;
        agent.speed = chaseSpeed;
    }

    private void UpdateChasing()
    {
        if (visionCone.playerRef == null) return;

        var eye = transform.position + Vector3.up * 0.8f;
        var target = visionCone.playerRef.transform.position + Vector3.up * 0.5f;
        var dist = Vector3.Distance(eye, target);

        var combinedMask = visionCone.obstructionMask | LayerMask.GetMask("Target") | LayerMask.GetMask("Interactable");
        RaycastHit hit;
        var hasLoS = false;

        if (Physics.Raycast(eye, (target - eye).normalized, out hit, dist + 0.5f, combinedMask))
            if (hit.collider.CompareTag(playerTag))
                hasLoS = true;

        Debug.DrawLine(eye, target, hasLoS ? Color.red : Color.green);

        if (hasLoS && dist < visionCone.radius * 1.2f)
        {
            agent.SetDestination(visionCone.playerRef.transform.position);
            
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
            {
                GameEvents.OnSuspicionAdded?.Invoke(suspicionBuildSpeed * Time.deltaTime);
            }
        }
        else
        {
            var reachedSpot = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
            var isStuck = agent.velocity.sqrMagnitude < 0.1f;

            if (reachedSpot || isStuck) EnterSearching();
        }
    }

    // ─── STATE 4: SEARCHING ──────────────────────────────────────────────
    
    private void EnterSearching()
    {
        currentState = State.Searching;
        UpdateVisuals("?", searchingColor);
        agent.isStopped = true; 
        if (anim != null) anim.SetBool("IsWalking", false);
        if (anim != null) anim.SetBool("LookAround", true); 
        searchTimer = 0f;
        searchStartRotation = transform.rotation; 
    }

    private void UpdateSearching()
    {
        searchTimer += Time.deltaTime;

        if (visionCone.canSeePlayer)
        {
            EnterChasing();
            return;
        }

        if (searchTimer >= searchDuration) GiveUpChase();
    }
    
    // ─── STATE 5: EVICTING & THROWING ────────────────────────────────────
    
    private void EnterEvicting()
    {
        CancelInvoke();
        currentState = State.Evicting;
        UpdateVisuals("GOTCHA!", Color.red);

        agent.isStopped = false;
        agent.speed = patrolSpeed; 
        agent.SetDestination(kickOutPoint.position);

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

        Vector3 frontOffset = transform.forward * 0.7f; 
        Vector3 targetHoldPos = transform.position + frontOffset;
        targetHoldPos.y = playerToEvict.transform.position.y; 

        playerToEvict.transform.position = Vector3.Lerp(playerToEvict.transform.position, targetHoldPos, Time.deltaTime * 25f);
        playerToEvict.transform.rotation = transform.rotation;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            ThrowPlayer(); 
        }
    }

    private void ThrowPlayer()
    {
        if (playerToEvict != null)
        {
            if (playerToEvict.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false; 
                Vector3 throwDirection = transform.forward * 4f + Vector3.up * 3f;
                rb.AddForce(throwDirection, ForceMode.Impulse);
            }

            if (playerToEvict.TryGetComponent(out Animator playerAnim))
            {
                playerAnim.Play("Fall", 0, 0f); 
            }

            StartCoroutine(RestorePlayerControlAfterFlight(playerToEvict));
        }

        graceTimer = postEvictionGrace; 
        playerToEvict = null;
        EnterPatrol();
    }

    private IEnumerator RestorePlayerControlAfterFlight(GameObject player)
    {
        yield return new WaitForSeconds(1.2f);

        if (player != null)
        {
            if (player.TryGetComponent(out CharacterController cc)) cc.enabled = true;
            if (player.TryGetComponent(out TopDownPlayerController controller)) controller.enabled = true;
            if (player.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true; 
        }
    }
    
    // ─── STATE 6: SUSPICIOUS (FOOTPRINTS) ────────────────────────────────
    
    private void EnterSuspicious(GameObject footprint)
    {
        currentState = State.Suspicious;
        UpdateVisuals("EW!", alertedColor);
        
        agent.isStopped = true;
        stopTimer = stopDuration;

        if (ewSound != null)
        {
            audioSource.PlayOneShot(ewSound);
        }

        if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
        {
            GameEvents.OnSuspicionAdded?.Invoke(footprintSuspicionPenalty);
        }

        StartCoroutine(IgnorePrintTemporary(footprint));
    }

    private void UpdateSuspicious()
    {
        stopTimer -= Time.deltaTime;

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
        yield return new WaitForSeconds(10f); 
        reactedPrints.Remove(print);
    }
    
    private void GiveUpChase()
    {
        if (anim != null) anim.SetBool("LookAround", false); 
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