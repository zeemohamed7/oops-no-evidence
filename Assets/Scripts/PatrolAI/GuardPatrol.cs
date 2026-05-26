using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    // Waypoints
    public Transform[] waypoints;
    private NavMeshAgent agent;
    private Vector3 target;
    private int waypointIndex;

    // Awake happens BEFORE Start, ensuring the agent is found immediately
    private GuardStateMachine stateMachine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stateMachine = GetComponent<GuardStateMachine>(); 
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Safety check: wait until the end of the frame or check if placed
        if (agent.isOnNavMesh) UpdateDestination();
    }

    // Update is called once per frame
    private void Update()
    {
        if (agent == null) return;
        // If the state machine is busy chasing or alerted, stop waypoint logic
        if (stateMachine.currentState != GuardStateMachine.State.Patrolling) return;
        // If reached, update
        if (Vector3.Distance(transform.position, target) < 1)
        {
            IterateWaypointIndex();
            UpdateDestination();
        }
    }

    public void StopPatrol()
    {
        if (agent != null) agent.isStopped = true;
    }

    public void ResumePatrol()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            SetClosestWaypoint();
            UpdateDestination();
        }
    }

    // Update target position and set agent's destination to that target
    private void UpdateDestination()
    {
        if (waypoints.Length == 0 || agent == null) return;

        target = waypoints[waypointIndex].position;
        agent.SetDestination(target);
    }

    // Increase waypoint index to go to the next one
    private void IterateWaypointIndex()
    {
        waypointIndex++;
        if (waypointIndex >= waypoints.Length) waypointIndex = 0;
    }

    private void SetClosestWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        var closestDistance = Mathf.Infinity;
        var closestIndex = 0;

        // Loop through all waypoints to find the one with the smallest distance
        for (var i = 0; i < waypoints.Length; i++)
        {
            var distance = Vector3.Distance(transform.position, waypoints[i].position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        // Set our current path to the one we found
        waypointIndex = closestIndex;
    }
}