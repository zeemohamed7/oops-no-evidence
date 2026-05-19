using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class NPCWalker : MonoBehaviour
{
    public Transform[] waypoints;
    public float walkSpeed = 1.5f;
    public float minWait = 2f; 
    public float maxWait = 6f; 

    [Header("Audio")]
    public AudioClip hmmClip;
    private AudioSource audioSource;
    private bool playedHmm = false;
    
    [Header("Suspicion Tuning")]
    public float suspicionAddSpeed = 20f; 

    private NavMeshAgent agent;
    private Animator anim;
    private int index = 0;
    private bool isBusy = false;
    private bool isLingering = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); 
        
        agent.speed = walkSpeed;
        agent.stoppingDistance = 0.3f; 

        ShuffleLookPoints();
        
        if (waypoints.Length > 0) MoveToNext();
    }

void Update()
    {
        if (SuspicionMeter.Instance == null) return;

        // 1. Panic Breakout
        if (SuspicionMeter.Instance.CurrentState == SuspicionMeter.SuspicionState.Panic)
        {
            TriggerPanicEscape();
            
            // Move the panic freeze code inside this block so it CANNOT touch the normal loop!
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                anim.SetFloat("Vert", 0f); 
                anim.SetFloat("State", 1f); 
            }
            return; 
        }

        // 2. Vision Check
        VisionCone vision = GetComponent<VisionCone>();
        if (vision != null && vision.canSeePlayer && !isLingering && !isBusy)
        {
            StartCoroutine(LingerAndStare());
        }

        // 3. Update Movement Animations
        if (!isBusy && !isLingering) 
        {
            float currentSpeed = agent.velocity.magnitude; 
            anim.SetFloat("Vert", currentSpeed > 0.1f ? 1f : 0f); 
            anim.SetFloat("Hor", 0f);
            anim.SetFloat("State", 0f); 
        }
        
        // 4. Arrival Check (Safely separated)
        if (!isBusy && !isLingering && waypoints.Length > 0)
        {
            // Physically calculate how far the NPC is from the exact target node
            float distanceToTarget = Vector3.Distance(transform.position, waypoints[index].position);

            // If he steps within 0.5 meters of it, immediately trigger the next spot
            if (distanceToTarget <= 0.5f)
            {
                StartCoroutine(HandleWaypoint());
            }
        }
    }

    IEnumerator LingerAndStare()
    {
        isLingering = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Play Sound Once
        if (hmmClip != null && audioSource != null && !playedHmm)
        {
            audioSource.PlayOneShot(hmmClip);
            playedHmm = true;
        }

        // Look Around / Suspicious Animation
        anim.SetFloat("State", 1f);
        anim.SetFloat("Vert", 0f);

        // Stand there staring and adding suspicion over time
        float timer = 0f;
        while (timer < 3.5f)
        {
            // If he still sees you, feed the global suspicion meter event
            VisionCone vision = GetComponent<VisionCone>();
            if (vision != null && vision.canSeePlayer)
            {
                GameEvents.OnSuspicionAdded?.Invoke(suspicionAddSpeed * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null; // Wait 1 frame
        }

        // Resume walking if we didn't hit panic
        if (SuspicionMeter.Instance.CurrentState != SuspicionMeter.SuspicionState.Panic)
        {
            anim.SetFloat("State", 0f);
            agent.isStopped = false;
        }

        isLingering = false;
    }

    IEnumerator HandleWaypoint()
    {
        isBusy = true;
    
        // 1. Figure out if this spot needs an idle animation pause
        Transform completedPoint = waypoints[index];
        if (completedPoint.gameObject.name.Contains("LookPoint"))
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat("State", 1f); 
            anim.SetFloat("Vert", 0f);

            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
    
            anim.SetFloat("State", 0f); 
            yield return new WaitForSeconds(0.2f); 
        }

        // 2. Immediately increment to the next index BEFORE moving
        // This breaks the infinite loop instantly!
        index = (index + 1) % waypoints.Length;

        // 3. Command the agent to start walking to the next clean position
        agent.isStopped = false;
        MoveToNext();

        // 4. Give him a split second to walk away from the old point so he doesn't re-trigger it
        yield return new WaitForSeconds(0.5f); 
    
        isBusy = false;
    }

    void TriggerPanicEscape()
    {
        StopAllCoroutines(); // Clears all wait timers
        isBusy = false;
        isLingering = false;

        agent.isStopped = false;
        agent.speed = walkSpeed * 2.5f; // Fast sprint speed
    
        anim.SetFloat("State", 1f);
        anim.SetFloat("Vert", 1f);

        index = 1; // Out of sight waypoint
        agent.SetDestination(waypoints[1].position);
    }

    void MoveToNext()
    {
        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[index].position);
    }

    void ShuffleLookPoints()
    {
        List<Transform> lookPointTransforms = new List<Transform>(); 
        List<Vector3> positions = new List<Vector3>(); 

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint.name.Contains("LookPoint"))
            {
                lookPointTransforms.Add(waypoint);
                positions.Add(waypoint.position);
            }
        }

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 temp = positions[i];
            int randomIndex = Random.Range(i, positions.Count);
            positions[i] = positions[randomIndex];
            positions[randomIndex] = temp;
        }

        for (int i = 0; i < lookPointTransforms.Count; i++)
        {
            lookPointTransforms[i].position = positions[i];
        }
    }
}