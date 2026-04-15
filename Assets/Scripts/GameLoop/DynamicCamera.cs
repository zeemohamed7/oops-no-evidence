using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// DynamicCamera
// ─────────────────────────────────────────────────────────────────────────────
// Attach to the Camera in the gameplay scene.
// The camera is ORTHOGRAPHIC and looks straight down (CCTV view, like Overcooked).
//
// Setup in Unity:
//   • Set Camera → Projection to Orthographic.
//   • Rotate the Camera to (90, 0, 0) so it looks straight down.
//   • Place it high on the Y axis (e.g. Y = 20).
//   • Assign this component; leave players list empty — PlayerSpawner fills it.
// ─────────────────────────────────────────────────────────────────────────────
[RequireComponent(typeof(Camera))]
public class DynamicCamera : MonoBehaviour
{
    public static DynamicCamera Instance;

    [Header("Follow")]
    [Tooltip("How quickly the camera glides to the target position.")]
    public float positionSmoothSpeed = 5f;

    [Tooltip("Fixed Y height above the scene. Leave at 0 to keep the Camera's starting Y.")]
    public float cameraHeight = 20f;

    [Header("Zoom (Orthographic Size)")]
    [Tooltip("Smallest ortho size — closest the camera can get.")]
    public float minOrthoSize = 4f;

    [Tooltip("Largest ortho size — farthest the camera can pull out.")]
    public float maxOrthoSize = 14f;

    [Tooltip("Extra world-unit padding added around the group of players.")]
    public float padding = 2.5f;

    [Tooltip("How quickly the zoom transitions.")]
    public float zoomSmoothSpeed = 4f;

    // ── internals ────────────────────────────────────────────────────────────

    private Camera cam;
    private readonly List<Transform> players = new List<Transform>();

    // ── lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    private void LateUpdate()
    {
        if (players.Count == 0) return;

        // ── position ─────────────────────────────────────────────────────────
        Vector3 center = GetCenter();
        Vector3 targetPos = new Vector3(center.x, cameraHeight, center.z);

        transform.position = Vector3.Lerp(transform.position, targetPos,
                                          positionSmoothSpeed * Time.deltaTime);

        // ── zoom ─────────────────────────────────────────────────────────────
        float targetSize = GetRequiredOrthoSize(center);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize,
                                          zoomSmoothSpeed * Time.deltaTime);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Vector3 GetCenter()
    {
        Vector3 sum = Vector3.zero;
        foreach (Transform p in players) sum += p.position;
        return sum / players.Count;
    }

    private float GetRequiredOrthoSize(Vector3 center)
    {
        float maxDist = 0f;
        foreach (Transform p in players)
        {
            // Only care about XZ spread (we're top-down).
            float dist = Vector2.Distance(
                new Vector2(p.position.x, p.position.z),
                new Vector2(center.x,     center.z));
            if (dist > maxDist) maxDist = dist;
        }

        // Also account for the screen's aspect ratio so wide spreads fit.
        float sizeBySpread = maxDist + padding;
        float sizeForAspect = sizeBySpread / cam.aspect;
        float required = Mathf.Max(sizeBySpread, sizeForAspect);

        return Mathf.Clamp(required, minOrthoSize, maxOrthoSize);
    }

    // ── public API ───────────────────────────────────────────────────────────

    /// <summary>Call this from PlayerSpawner when a player joins the scene.</summary>
    public void RegisterPlayer(Transform t)
    {
        if (t != null && !players.Contains(t))
            players.Add(t);
    }

    /// <summary>Call this if a player leaves mid-game.</summary>
    public void UnregisterPlayer(Transform t)
    {
        players.Remove(t);
    }

    /// <summary>Snap instantly to the group (useful at scene start to avoid a slow pan-in).</summary>
    public void SnapToPlayers()
    {
        if (players.Count == 0) return;
        Vector3 center = GetCenter();
        transform.position = new Vector3(center.x, cameraHeight, center.z);
        cam.orthographicSize = GetRequiredOrthoSize(center);
    }
}
