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
        var rangeChecks =
            Physics.OverlapSphere(transform.position, radius,
                targetMask); // Mask is look at that layer for that object (which is player)


        if (rangeChecks.Length != 0) // Found something on that layer
        {
            var target =
                rangeChecks[0].transform; // OverlapSphere returns array and this layer only has 1 object anyway
            var directionToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2) // Angle we should be looking
            {
                // Is distance close enough
                var distanceToTarget = Vector3.Distance(transform.position, target.position);

                // Raycast to determine whether we can see player
                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget,
                        obstructionMask)) // NOT because we are not hitting obstruction mask, therefore we can see player
                    canSeePlayer = true;

                else
                    canSeePlayer = false;
            }
            else
            {
                canSeePlayer = false;
            }
        }
        else // If player was in FOV and no longer, update it
        {
            if (canSeePlayer)
                canSeePlayer = false;
        }
    }
}