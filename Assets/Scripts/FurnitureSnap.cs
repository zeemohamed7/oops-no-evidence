using UnityEngine;

public class FurnitureSnap : MonoBehaviour
{
    private FurnitureItem furnitureItem;
    private Outline outline;

    [Header("Snap Settings")]
    public float snapDistance = 3f;
    public float snapRotation = 45f;

    [Header("Outline Colors")]
    public Color wrongColor = Color.red;
    public Color closeColor = Color.yellow;

    private bool snapped = false;
    public bool IsSnapped => snapped;

    void Awake()
    {
        furnitureItem = GetComponent<FurnitureItem>();
        outline = GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = wrongColor;
        }
    }

    void Update()
    {
        if (snapped || furnitureItem == null || outline == null)
            return;

        float distance = Vector3.Distance(transform.position, furnitureItem.OriginalPosition);
        float angle = Quaternion.Angle(transform.rotation, furnitureItem.OriginalRotation);

        bool closeEnough = distance <= snapDistance && angle <= snapRotation;

        outline.enabled = true;
        outline.OutlineColor = closeEnough ? closeColor : wrongColor;
    }

    public void TrySnap()
    {
        if (furnitureItem == null) return;

        float distance = Vector3.Distance(transform.position, furnitureItem.OriginalPosition);
        float angle = Quaternion.Angle(transform.rotation, furnitureItem.OriginalRotation);

        if (distance <= snapDistance && angle <= snapRotation)
        {
            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            transform.position = furnitureItem.OriginalPosition;
            transform.rotation = furnitureItem.OriginalRotation;

            snapped = true;

            if (outline != null)
                outline.enabled = false;

            Debug.Log(name + " SNAPPED SUCCESSFULLY");
            CheckAllSnapped();
        }
        else
        {
            Debug.Log(name + " did NOT snap");
        }
    }

    void CheckAllSnapped()
    {
        foreach (var snap in FindObjectsByType<FurnitureSnap>(FindObjectsSortMode.None))
            if (!snap.IsSnapped) return;

        GameEvents.OnTaskCompleted?.Invoke("rearrange_furniture");
    }

    public void MarkPickedUpAgain()
    {
        snapped = false;

        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = wrongColor;
        }
    }
}