using UnityEngine;
using UnityEngine.AI;

public class GuardPatrol : MonoBehaviour
{
    // Waypoints
    public Transform[] waypoints;
    private NavMeshAgent agent;
    private Vector3 target;
    private int waypointIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        UpdateDestination();
    }

    // Update is called once per frame
    private void Update()
    {
        // If reached, update
        if (Vector3.Distance(transform.position, target) < 1)
        {
            IterateWaypointIndex();
            UpdateDestination();
        }
    }

    public void StopPatrol()
    {
        agent.isStopped = true;
    }

    public void ResumePatrol()
    {
        agent.isStopped = false;
        UpdateDestination();
    }

    // Update target position and set agent's destination to that target
    private void UpdateDestination()
    {
        target = waypoints[waypointIndex].position;
        agent.SetDestination(target);
    }

    // Increase waypoint index to go to the next one
    private void IterateWaypointIndex()
    {
        waypointIndex++;
        if (waypointIndex >= waypoints.Length) waypointIndex = 0;
    }
}