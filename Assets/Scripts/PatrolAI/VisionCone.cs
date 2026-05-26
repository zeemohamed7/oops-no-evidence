using System.Collections;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    public float radius;
    [Range(0, 360)] public float angle;

    public GameObject playerRef;

    // Dealing with targets and walls
    public LayerMask targetMask;
    public LayerMask obstructionMask;
    public bool canSeePlayer;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
    }

    // Look for player, wait 5 times every second for performance
    private IEnumerator FOVRoutine()
    {
        var wait = new WaitForSeconds(0.2f); // Coroutine to Pauses and resumes after 0.2f
        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        // Finds everything on the threat layer
        var rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask); 

        if (rangeChecks.Length != 0) 
        {
            bool spottedSomething = false;

            // Loop through everything found instead of just looking at item [0]
            foreach (var check in rangeChecks)
            {
                var target = check.transform; 
                var directionToTarget = (target.position - transform.position).normalized;
            
                if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2) 
                {
                    var distanceToTarget = Vector3.Distance(transform.position, target.position);

                    // Added a slight lift (+ Vector3.up) so raycasts don't hit the floor grid
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToTarget, distanceToTarget, obstructionMask)) 
                    {
                        spottedSomething = true;
                        break; // We found a threat! Stop looking at the rest
                    }
                }
            }
            canSeePlayer = spottedSomething; // True if they see player OR blood
            
            if (canSeePlayer)
            {
                float suspicionAmount = 20f * 0.2f; // 20 per second * 0.2s interval
                GameEvents.OnSuspicionAdded?.Invoke(suspicionAmount);
            }
        }
        else 
        {
            canSeePlayer = false;
        }
    }
    
// Draw the vision cone area directly in the Scene view for easy debugging
    private void OnDrawGizmos()
    {
        // 1. Draw the maximum range circle/sphere boundary
        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        // 2. Draw the left and right borders of the field of view angle
        Vector3 forward = transform.forward;
        
        // Calculate the boundary vectors based on the angle
        Vector3 leftRayDirection = Quaternion.Euler(0, -angle / 2, 0) * forward;
        Vector3 rightRayDirection = Quaternion.Euler(0, angle / 2, 0) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, leftRayDirection * radius);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rightRayDirection * radius);
    }
}