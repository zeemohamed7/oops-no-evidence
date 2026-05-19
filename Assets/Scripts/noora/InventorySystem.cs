using UnityEngine;

/// <summary>
/// Inventory: Bucket(0), Mop(1), Spray(2), Blacklight(3)
///
/// Rules:
///   - Only one hand-tool equipped at a time (Mop, Spray, Blacklight).
///   - Bucket is a FLOOR object — never held. Selecting it just marks it as
///     "active/nearby". It can be active at the same time as a hand-tool.
///   - Pressing the same key again deselects that item.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    [Header("Hand Tools  (shown in player's hand)")]
    public GameObject mop;
    public GameObject spray;
    public GameObject blacklight;

    [Header("Bucket  (floor object — never held)")]
    [Tooltip("The bucket GameObject sitting on the floor in the scene.")]
    public GameObject bucket;

    // Slot constants
    public const int SLOT_BUCKET = 0;
    public const int SLOT_MOP = 1;
    public const int SLOT_SPRAY = 2;
    public const int SLOT_BLACKLIGHT = 3;

    // -1 = nothing
    private int _handSlot = -1;   // currently held hand-tool
    private bool _bucketActive = false; // bucket selected (on floor)
    private PlayerAnimationDriver animationDriver;

    void Start()
    {
        animationDriver = GetComponent<PlayerAnimationDriver>();
        HideAllHandTools();
        // Bucket is hidden until player presses 1
        if (bucket) bucket.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleBucket();
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleHandTool(SLOT_MOP, mop);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleHandTool(SLOT_SPRAY, spray);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleHandTool(SLOT_BLACKLIGHT, blacklight);
    }

    // ── Bucket (floor object) ──────────────────────────────────────────────

    void ToggleBucket()
    {
        _bucketActive = !_bucketActive;
        if (bucket) bucket.SetActive(_bucketActive);
        Debug.Log(_bucketActive ? "[Inventory] Bucket placed on floor."
                                : "[Inventory] Bucket hidden.");
    }

    // ── Hand tools ─────────────────────────────────────────────────────────

    void ToggleHandTool(int slot, GameObject toolObj)
    {
        if (_handSlot == slot)
        {
            if (toolObj) toolObj.SetActive(false);
            _handSlot = -1;

            animationDriver?.ClearSelectedItem();

            Debug.Log($"[Inventory] Slot {slot} unequipped.");
        }
        else
        {
            HideAllHandTools();

            if (toolObj)
            {
                toolObj.SetActive(true);
                _handSlot = slot;

                if (slot == SLOT_MOP)
                    animationDriver?.SelectMop();
                else
                    animationDriver?.SelectTool();

                Debug.Log($"[Inventory] Equipped slot {slot}.");
            }
            else
            {
                Debug.LogWarning($"[Inventory] Slot {slot} has no GameObject assigned!");
            }
        }
    }

    void HideAllHandTools()
    {
        if (mop) mop.SetActive(false);
        if (spray) spray.SetActive(false);
        if (blacklight) blacklight.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Currently held hand-tool slot index, or -1 if empty-handed.</summary>
    public int GetHandSlot() => _handSlot;

    /// <summary>True when the bucket has been selected by the player.</summary>
    public bool IsBucketActive() => _bucketActive;

    /// <summary>Deactivate bucket selection (called by MopCleaner after dipping).</summary>
    public void DeselectBucket()
    {
        _bucketActive = false;
        if (bucket) bucket.SetActive(false);
        Debug.Log("[Inventory] Bucket hidden after dip.");
    }
}