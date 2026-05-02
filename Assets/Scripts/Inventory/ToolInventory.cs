using UnityEngine;
using UnityEngine.UI;

public class ToolInventory : MonoBehaviour
{
    [Header("Slot Highlight Images")]
    public Image slot1Highlight;
    public Image slot2Highlight;
    public Image slot3Highlight;
    public Image slot4Highlight;

    [Header("Tool Objects On Player")]
    public GameObject mopTool;
    public GameObject bucketTool;
    public GameObject blacklightTool;
    public GameObject sprayTool;

    private int selectedSlot = 1;

    void Start()
    {
        SelectSlot(1);
    }

    void Update()
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

        slot1Highlight.gameObject.SetActive(slotNumber == 1);
        slot2Highlight.gameObject.SetActive(slotNumber == 2);
        slot3Highlight.gameObject.SetActive(slotNumber == 3);
        slot4Highlight.gameObject.SetActive(slotNumber == 4);

        if (mopTool != null) mopTool.SetActive(slotNumber == 1);
        if (bucketTool != null) bucketTool.SetActive(slotNumber == 2);
        if (blacklightTool != null) blacklightTool.SetActive(slotNumber == 3);
        if (sprayTool != null) sprayTool.SetActive(slotNumber == 4);

        Debug.Log("Selected Slot: " + slotNumber);
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
}

