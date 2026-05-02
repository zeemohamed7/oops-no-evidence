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

    private int selectedSlot = 0;
    private float highlightDuration = 0.5f;

    void Start()
    {
        ClearSelection();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(4);

        float scroll = Input.mouseScrollDelta.y;

        if (scroll > 0)
        {
            selectedSlot--;
            if (selectedSlot < 1) selectedSlot = 4;
            SelectSlot(selectedSlot);
        }
        else if (scroll < 0)
        {
            selectedSlot++;
            if (selectedSlot > 4) selectedSlot = 1;
            SelectSlot(selectedSlot);
        }
    }

    void SelectSlot(int slotNumber)
    {
        selectedSlot = slotNumber;

        Color glowColor = new Color(0.6f, 0.2f, 1f, 0.6f); 

        slot1Highlight.gameObject.SetActive(slotNumber == 1);
        slot2Highlight.gameObject.SetActive(slotNumber == 2);
        slot3Highlight.gameObject.SetActive(slotNumber == 3);
        slot4Highlight.gameObject.SetActive(slotNumber == 4);

        slot1Highlight.color = glowColor;
        slot2Highlight.color = glowColor;
        slot3Highlight.color = glowColor;
        slot4Highlight.color = glowColor;

        if (mopTool != null) mopTool.SetActive(slotNumber == 1);
        if (bucketTool != null) bucketTool.SetActive(slotNumber == 2);
        if (blacklightTool != null) blacklightTool.SetActive(slotNumber == 3);
        if (sprayTool != null) sprayTool.SetActive(slotNumber == 4);

        // UpdateSlotScale();
        StopAllCoroutines();
        StartCoroutine(HideHighlightAfterDelay());

        Debug.Log("Selected Slot: " + slotNumber);
    }

        void ClearSelection()
    {
        slot1Highlight.gameObject.SetActive(false);
        slot2Highlight.gameObject.SetActive(false);
        slot3Highlight.gameObject.SetActive(false);
        slot4Highlight.gameObject.SetActive(false);

        if (mopTool != null) mopTool.SetActive(false);
        if (bucketTool != null) bucketTool.SetActive(false);
        if (blacklightTool != null) blacklightTool.SetActive(false);
        if (sprayTool != null) sprayTool.SetActive(false);

        slot1Highlight.transform.parent.localScale = Vector3.one;
        slot2Highlight.transform.parent.localScale = Vector3.one;
        slot3Highlight.transform.parent.localScale = Vector3.one;
        slot4Highlight.transform.parent.localScale = Vector3.one;
    }

    public int GetSelectedSlot()
    {
        return selectedSlot;
    }

    public bool IsMopSelected()
    {
        return selectedSlot == 1;
    }

    public bool IsBucketSelected()
    {
        return selectedSlot == 2;
    }

    public bool IsBlacklightSelected()
    {
        return selectedSlot == 3;
    }

    public bool IsSpraySelected()
    {
        return selectedSlot == 4;
    }

    System.Collections.IEnumerator HideHighlightAfterDelay()
    {
        yield return new WaitForSeconds(highlightDuration);

        slot1Highlight.gameObject.SetActive(false);
        slot2Highlight.gameObject.SetActive(false);
        slot3Highlight.gameObject.SetActive(false);
        slot4Highlight.gameObject.SetActive(false);
    }
}
