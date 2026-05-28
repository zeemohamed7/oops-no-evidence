using System.Collections.Generic;
using UnityEngine;
public class FurnitureOrganizationChecker : MonoBehaviour
{
    [Header("Items To Check")]
    [Tooltip("Drag all FurnitureItem objects that must be returned to their original positions.")]
    public List<FurnitureItem> items = new List<FurnitureItem>();

    [Header("Debug")]
    public bool showGizmosInEditor = true;
    
    /// <param name="verbose">Set true only for one-shot checks (e.g. extraction). Leave false for every-frame polling to avoid console spam.</param>
    public bool IsOrganized(float posTolerance, float rotTolerance, bool verbose = false)
    {
        if (items.Count == 0)
        {
            if (verbose)
                Debug.LogWarning($"[FurnitureOrganizationChecker] '{name}': items list is EMPTY — check passes vacuously. " +
                                 "Drag all FurnitureItem scene objects into the 'Items' list in the Inspector.");
            return true;
        }

        bool allGood = true;
        foreach (FurnitureItem item in items)
        {
            if (item == null) continue;

            FurnitureSnap snap = item.GetComponent<FurnitureSnap>();

            if (snap != null && !snap.IsBeingRearranged && !snap.IsSolved)
                continue;

            float dist = Vector3.Distance(item.transform.position, item.OriginalPosition);
            float angle = Quaternion.Angle(item.transform.rotation, item.OriginalRotation);

            bool pass = dist <= posTolerance && angle <= rotTolerance;

            if (!pass)
                allGood = false;
        }
        return allGood;
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
