using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// Using Finite State Machine (FSM) to make sure guard is only ever doing exactly one behavior at a time
public class GuardStateMachine : MonoBehaviour
{
    public float patrolSpeed = 2.5f;

    public float chaseSpeed = 5f;
    public float losePlayerTime = 3f;

    [Header("Global Suspicion Rates")] public float suspicionIncreaseRate = 25f;

    public float suspicionDrainRate = 10f;

    public UnityEvent OnPlayerDetected;

    public UnityEvent OnPlayerLost;

    private NavMeshAgent agent;

    private State currentState;
    private float loseTimer;
    private GuardPatrol patrol;
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
        }
    }

    // --- STATE 1: PATROLLING ---
    private void EnterPatrol()
    {
        currentState = State.Patrolling;
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
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        loseTimer = 0f;
    }

    private void UpdateChasing()
    {
        if (visionCone.playerRef == null) return;

        if (visionCone.canSeePlayer)
        {
            // PLAYER IS IN SIGHT: Chase them and increase the global Game Over bar
            agent.SetDestination(visionCone.playerRef.transform.position);
            loseTimer = 0f;
            SuspicionMeter.Instance.ModifySuspicion(suspicionIncreaseRate * Time.deltaTime);
        }
        else
        {
            // PLAYER IS NOT IN SIGHT: Start the "Give up" timer and drain the global Game Over bar
            loseTimer += Time.deltaTime;
            SuspicionMeter.Instance.ModifySuspicion(-suspicionDrainRate * Time.deltaTime);

            if (loseTimer >= losePlayerTime) GiveUpChase();
        }
    }

    private void GiveUpChase()
    {
        loseTimer = 0f;
        OnPlayerLost.Invoke();
        EnterPatrol();
    }

    private enum State
    {
        Patrolling,
        Alerted,
        Chasing
    }
}