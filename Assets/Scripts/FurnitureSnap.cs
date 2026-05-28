using UnityEngine;

public class FurnitureSnap : MonoBehaviour
{
    private FurnitureItem furnitureItem;
    private Outline outline;
    private Rigidbody rb;

    [Header("Snap Settings")]
    public float snapDistance = 3f;
    public float snapRotation = 45f;

    [Header("Outline Colors")]
    public Color wrongColor = Color.red;
    public Color closeColor = Color.yellow;
    public Color correctColor = Color.green;

    public bool IsSolved { get; private set; }
    public bool IsBeingRearranged => isBeingRearranged;
    private bool isBeingRearranged = false;

        public enum RearrangeType
        {
            StartsAsMess,
            ActivatedWhenPushed
        }

        [Header("Rearrange Type")]
        public RearrangeType rearrangeType;

    void Awake()
    {
        furnitureItem = GetComponent<FurnitureItem>();
        outline = GetComponent<Outline>();
        rb = GetComponent<Rigidbody>();

        isBeingRearranged = false;
        IsSolved = false;

        if (rearrangeType == RearrangeType.StartsAsMess)
        {
            EnterRearrangeMode();
        }
        else
        {
            if (outline != null)
                outline.enabled = false;
        }
    }

    void Update()
    {
        if (!isBeingRearranged || IsSolved || furnitureItem == null || outline == null)
            return;

        float distance = Vector3.Distance(transform.position, furnitureItem.OriginalPosition);
        float angle = Quaternion.Angle(transform.rotation, furnitureItem.OriginalRotation);

        bool closeEnough = distance <= snapDistance && angle <= snapRotation;

        outline.enabled = true;
        outline.OutlineColor = closeEnough ? closeColor : wrongColor;
    }

    public void EnterRearrangeMode()
    {
        if (IsSolved) return;

        isBeingRearranged = true;

        if (outline != null)
        {
            outline.enabled = true;
            outline.OutlineColor = wrongColor;
        }
    }

    public void TrySnap()
    {
        if (furnitureItem == null) return;
        if (IsSolved) return;

        float distance = Vector3.Distance(transform.position, furnitureItem.OriginalPosition);
        float angle = Quaternion.Angle(transform.rotation, furnitureItem.OriginalRotation);

        if (distance <= snapDistance && angle <= snapRotation)
        {
            PushPlayersAway();
            transform.position = furnitureItem.OriginalPosition;
            transform.rotation = furnitureItem.OriginalRotation;

            IsSolved = true;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (outline != null)
            {
                outline.enabled = false;
                // outline.OutlineColor = correctColor;
            }

            Debug.Log(name + " solved and locked.");
        }
        else
        {
            Debug.Log(name + " did NOT snap.");
        }
    }

    private void PushPlayersAway()
    {
        Collider[] hits = Physics.OverlapSphere(
            furnitureItem.OriginalPosition,
            1.5f
        );

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                continue;

            CharacterController cc = hit.GetComponent<CharacterController>();

            if (cc == null)
                continue;

            Vector3 pushDir =
                hit.transform.position - furnitureItem.OriginalPosition;

            pushDir.y = 0;

            if (pushDir.sqrMagnitude < 0.01f)
                pushDir = -hit.transform.forward;

            pushDir.Normalize();

            cc.enabled = false;

            hit.transform.position += pushDir * 1.5f;

            cc.enabled = true;
        }
    }
}