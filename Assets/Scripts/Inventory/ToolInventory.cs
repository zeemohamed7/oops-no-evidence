using UnityEngine;
using UnityEngine.UI;

public class ToolInventory : MonoBehaviour
{
    public Image slot1Highlight;
    public Image slot2Highlight;
    public Image slot3Highlight;
    public Image slot4Highlight;
    public GameObject mopTool;
    public GameObject bucketTool;
    public GameObject blacklightTool;
    public GameObject sprayTool;
    public MopCleaner mopCleaner;
    public float dipDistance = 1.5f;

    private int selectedSlot = -1; // -1 = nothing equipped
    private float highlightDuration = 0.5f;
    private bool bucketVisible = false;

    void Start()
    {
        ClearSelection();
        if (bucketTool != null) bucketTool.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        CheckBucketProximity();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleHandTool(1, mopTool);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleBucket();
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleHandTool(3, blacklightTool);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ToggleHandTool(4, sprayTool);

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0) { selectedSlot--; if (selectedSlot < 1) selectedSlot = 4; HighlightSlot(selectedSlot); }
        else if (scroll < 0) { selectedSlot++; if (selectedSlot > 4) selectedSlot = 1; HighlightSlot(selectedSlot); }
    }

    void ToggleBucket()
    {
        bucketVisible = !bucketVisible;
        if (bucketTool != null) bucketTool.SetActive(bucketVisible);
        HighlightSlot(2);
        Vector3 offset = transform.right * 1f; // place to the right of player
        bucketTool.transform.position = new Vector3(
            transform.position.x + offset.x,
            bucketTool.transform.position.y, // keep original Y (floor level)
            transform.position.z + offset.z
        );
    }


    void ToggleHandTool(int slot, GameObject toolObj)
    {
        if (selectedSlot == slot)
        {
            // same slot pressed again — unequip
            if (toolObj) toolObj.SetActive(false);
            selectedSlot = -1;
            Debug.Log($"[Inventory] Slot {slot} unequipped.");
        }
        else
        {
            // switch tool — hide all hand tools first
            if (mopTool != null) mopTool.SetActive(false);
            if (blacklightTool != null) blacklightTool.SetActive(false);
            if (sprayTool != null) sprayTool.SetActive(false);

            if (toolObj) toolObj.SetActive(true);
            selectedSlot = slot;
            HighlightSlot(slot);
            Debug.Log($"[Inventory] Equipped slot {slot}.");
        }
    }

    void HighlightSlot(int slotNumber)
    {
        Color glowColor = new Color(0.6f, 0.2f, 1f, 0.6f);

        if (slot1Highlight != null) { slot1Highlight.gameObject.SetActive(slotNumber == 1); slot1Highlight.color = glowColor; }
        if (slot2Highlight != null) { slot2Highlight.gameObject.SetActive(slotNumber == 2); slot2Highlight.color = glowColor; }
        if (slot3Highlight != null) { slot3Highlight.gameObject.SetActive(slotNumber == 3); slot3Highlight.color = glowColor; }
        if (slot4Highlight != null) { slot4Highlight.gameObject.SetActive(slotNumber == 4); slot4Highlight.color = glowColor; }

        StopAllCoroutines();
        StartCoroutine(HideHighlightAfterDelay());
    }

    void CheckBucketProximity()
    {
        if (bucketTool == null || mopCleaner == null) return;
        if (!IsMopSelected()) return;

        float dist = Vector3.Distance(transform.position, bucketTool.transform.position);
        if (dist <= dipDistance)
            mopCleaner.TryDipMop();
    }

    void ClearSelection()
    {
        if (slot1Highlight != null) slot1Highlight.gameObject.SetActive(false);
        if (slot2Highlight != null) slot2Highlight.gameObject.SetActive(false);
        if (slot3Highlight != null) slot3Highlight.gameObject.SetActive(false);
        if (slot4Highlight != null) slot4Highlight.gameObject.SetActive(false);

        if (mopTool != null) mopTool.SetActive(false);
        if (bucketTool != null) bucketTool.SetActive(false);
        if (blacklightTool != null) blacklightTool.SetActive(false);
        if (sprayTool != null) sprayTool.SetActive(false);

        if (slot1Highlight != null) slot1Highlight.transform.parent.localScale = Vector3.one;
        if (slot2Highlight != null) slot2Highlight.transform.parent.localScale = Vector3.one;
        if (slot3Highlight != null) slot3Highlight.transform.parent.localScale = Vector3.one;
        if (slot4Highlight != null) slot4Highlight.transform.parent.localScale = Vector3.one;
    }

    public int GetSelectedSlot() => selectedSlot;
    public bool IsMopSelected() => selectedSlot == 1;
    public bool IsBucketSelected() => bucketVisible;
    public bool IsBlacklightSelected() => selectedSlot == 3;
    public bool IsSpraySelected() => selectedSlot == 4;

    System.Collections.IEnumerator HideHighlightAfterDelay()
    {
        yield return new WaitForSeconds(highlightDuration);
        if (slot1Highlight != null) slot1Highlight.gameObject.SetActive(false);
        if (slot2Highlight != null) slot2Highlight.gameObject.SetActive(false);
        if (slot3Highlight != null) slot3Highlight.gameObject.SetActive(false);
        if (slot4Highlight != null) slot4Highlight.gameObject.SetActive(false);
    }
}