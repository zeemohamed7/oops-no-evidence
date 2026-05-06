using UnityEngine;

// Top-down camera with per-level position. Set the fields below in the Inspector
// for each scene — rotation (90,0,0) for pure top-down, (60,45,0) for isometric.
[RequireComponent(typeof(Camera))]
public class DynamicCamera : MonoBehaviour
{
    public static DynamicCamera Instance;

    [Header("Level Camera Settings")]
    [Tooltip("World-space position of the camera for this level.")]
    public Vector3 cameraPosition = new Vector3(0f, 10f, 0f);

    [Tooltip("Euler angles for the camera rotation. (90,0,0) = pure top-down.")]
    public Vector3 cameraRotation = new Vector3(90f, 0f, 0f);

    [Tooltip("Orthographic size (only used when Camera.orthographic is true).")]
    public float orthographicSize = 8f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        transform.position = cameraPosition;
        transform.rotation = Quaternion.Euler(cameraRotation);

        Camera cam = GetComponent<Camera>();
        if (cam.orthographic)
            cam.orthographicSize = orthographicSize;
    }

    // Kept so existing callers (TopDownPlayerController) don't break at compile time.
    public void RegisterPlayer(Transform t) { }
    public void UnregisterPlayer(Transform t) { }
}
