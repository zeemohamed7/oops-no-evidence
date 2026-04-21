using UnityEngine;

// Fixed overhead camera — set position/rotation/ortho size in the Inspector.
// Rotation should be (60, 45, 0) for Overcooked-style isometric, or (90, 0, 0) for pure top-down.
[RequireComponent(typeof(Camera))]
public class DynamicCamera : MonoBehaviour
{
    public static DynamicCamera Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Kept so existing callers (TopDownPlayerController) don't break at compile time.
    public void RegisterPlayer(Transform t) { }
    public void UnregisterPlayer(Transform t) { }
}
