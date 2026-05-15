using System.Collections.Generic;
using UnityEngine;
public class FurnitureOrganizationChecker : MonoBehaviour
{
    [Header("Items To Check")]
    [Tooltip("Drag all FurnitureItem objects that must be returned to their original positions.")]
    public List<FurnitureItem> items = new List<FurnitureItem>();

    [Header("Debug")]
    public bool showGizmosInEditor = true;
    
    public bool IsOrganized(float posTolerance, float rotTolerance)
    {
        foreach (FurnitureItem item in items)
        {
            if (item == null) continue;
            if (!item.IsInOriginalPosition(posTolerance, rotTolerance))
                return false;
        }
        return true;
    }
    public List<FurnitureItem> GetDisorganizedItems(float posTolerance, float rotTolerance)
    {
        var result = new List<FurnitureItem>();
        foreach (FurnitureItem item in items)
        {
            if (item != null && !item.IsInOriginalPosition(posTolerance, rotTolerance))
                result.Add(item);
        }
        return result;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmosInEditor) return;
        foreach (FurnitureItem item in items)
        {
            if (item == null) continue;
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(item.OriginalPosition, Vector3.one * 0.6f);
        }
    }
#endif
}
