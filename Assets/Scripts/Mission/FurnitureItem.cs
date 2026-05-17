using UnityEngine;

/// <summary>
/// Attach to any furniture object that players can move and must return to its original position.
///
/// Setup:
///   1. Place furniture at its correct position in the scene.
///   2. Attach this component.
///   3. Right-click the component header → "Record Original Transform" to bake in the values.
///   4. The serialized values survive Play mode — you only need to re-record if you move the object in Edit mode.
/// </summary>
public class FurnitureItem : MonoBehaviour
{
    [Header("Original Transform")]
    [SerializeField] private Vector3 _originalPosition;
    [SerializeField] private Quaternion _originalRotation;
    [SerializeField] private bool _originalRecorded = false;

    void Awake()
    {
        if (!_originalRecorded)
            RecordOriginalTransform();
    }

    /// <summary>
    /// Bake the current world transform as the "original" state.
    /// Call this in the Inspector via right-click → context menu while in Edit mode.
    /// </summary>
    [ContextMenu("Record Original Transform")]
    public void RecordOriginalTransform()
    {
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalRecorded = true;
    }

    /// <summary>Returns true if this furniture is close enough to its original transform.</summary>
    public bool IsInOriginalPosition(float posTolerance, float rotTolerance)
    {
        float dist = Vector3.Distance(transform.position, _originalPosition);
        float angle = Quaternion.Angle(transform.rotation, _originalRotation);
        return dist <= posTolerance && angle <= rotTolerance;
    }

    public Vector3 OriginalPosition => _originalPosition;
    public Quaternion OriginalRotation => _originalRotation;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_originalRecorded) return;
        UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.7f);
        UnityEditor.Handles.DrawWireDisc(_originalPosition, Vector3.up, 0.5f);
        UnityEditor.Handles.Label(_originalPosition + Vector3.up * 0.6f, $"[{name}] target");
    }
#endif
}
