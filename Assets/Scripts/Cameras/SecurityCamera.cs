using System.Collections;
using UnityEngine;

/// <summary>
/// Main security camera script.
/// Attach to Camera_Parent alongside SecurityCameraVisionCone.cs.
/// Camera is static — no patrol, no rotation.
/// </summary>
public class SecurityCamera : MonoBehaviour
{
    [Header("Detection")]
    public float radius = 10f;
    [Range(0, 360)] public float angle = 90f;
    public LayerMask targetMask;
    public LayerMask obstructionMask;

    [Header("Suspicion")]
    [Tooltip("How much suspicion per second while player is in cone.")]
    public float suspicionPerSecond = 20f;

    // Public so SecurityCameraVisionCone can read it
    [HideInInspector] public bool canSeePlayer;
    [HideInInspector] public Transform playerTransform;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
        StartCoroutine(DetectionRoutine());
    }

    private IEnumerator DetectionRoutine()
    {
        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            yield return wait;
            CheckForPlayer();
        }
    }

    private void CheckForPlayer()
    {
        canSeePlayer = false;
        if (playerTransform == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer > radius) return;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;

        // Flatten both vectors to XZ so the camera tilt doesn't affect horizontal detection
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 flatDir     = new Vector3(dirToPlayer.x, 0f, dirToPlayer.z).normalized;

        if (Vector3.Angle(flatForward, flatDir) > angle / 2f) return;

        // Obstruction check
        if (Physics.Raycast(transform.position, dirToPlayer, distToPlayer, obstructionMask)) return;

        canSeePlayer = true;
    }

    private void Update()
    {
        if (canSeePlayer)
        {
            float suspicion = suspicionPerSecond * Time.deltaTime;
            GameEvents.OnSuspicionAdded?.Invoke(suspicion);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        Vector3 flat = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 left  = Quaternion.Euler(0, -angle / 2f, 0) * flat;
        Vector3 right = Quaternion.Euler(0,  angle / 2f, 0) * flat;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, left  * radius);
        Gizmos.DrawRay(transform.position, right * radius);
    }
}