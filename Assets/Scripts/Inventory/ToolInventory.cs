using UnityEngine;
using UnityEngine.InputSystem;

public class ToolInventory : MonoBehaviour
{
    [Header("Tools")]
    public GameObject mopTool;
    public GameObject bucketTool;
    public GameObject blacklightTool;
    public GameObject sprayTool;

    [Header("References")]
    public MopCleaner mopCleaner;
    public Transform holdPoint;

    private int selectedSlot = -1;
    private PlayerAnimationDriver animationDriver;

    // Per-player input actions — each player's PlayerInput component provides its own
    private InputAction _slot1;
    private InputAction _slot2;
    private InputAction _slot3;
    private InputAction _slot4;
    private InputAction _previous;
    private InputAction _next;

    void Start()
    {
        animationDriver = GetComponent<PlayerAnimationDriver>();

        var pi = GetComponent<PlayerInput>() ?? GetComponentInParent<PlayerInput>();
        if (pi != null)
        {
            _slot1    = FindAction(pi, "Slot1");
            _slot2    = FindAction(pi, "Slot2");
            _slot3    = FindAction(pi, "Slot3");
            _slot4    = FindAction(pi, "Slot4");
            _previous = FindAction(pi, "Previous");
            _next     = FindAction(pi, "Next");
        }
        else
        {
            Debug.LogWarning("[ToolInventory] No PlayerInput found — tool switching won't work.");
        }

        ClearSelection();
    }

    InputAction FindAction(PlayerInput pi, string name)
    {
        var action = pi.actions.FindAction(name);
        if (action == null)
            Debug.LogWarning($"[ToolInventory] Action '{name}' missing from Input Actions asset.");
        return action;
    }

    void Update()
    {
        HandleInput();
        EnforceSingleTool();
    }

    void HandleInput()
    {
        if (_slot1 != null && _slot1.WasPressedThisFrame()) ToggleHandTool(1, mopTool);
        if (_slot2 != null && _slot2.WasPressedThisFrame()) ToggleHandTool(2, bucketTool);
        if (_slot3 != null && _slot3.WasPressedThisFrame()) ToggleHandTool(3, blacklightTool);
        if (_slot4 != null && _slot4.WasPressedThisFrame()) ToggleHandTool(4, sprayTool);

        if (_previous != null && _previous.WasPressedThisFrame()) CycleSlot(-1);
        if (_next     != null && _next.WasPressedThisFrame())     CycleSlot(+1);
    }

    void CycleSlot(int dir)
    {
        int next = selectedSlot == -1 ? (dir > 0 ? 1 : 4) : selectedSlot + dir;
        if (next < 1) next = 4;
        if (next > 4) next = 1;
        selectedSlot = next;
    }

    // Runs every frame — keeps tool visibility in sync with selectedSlot
    void EnforceSingleTool()
    {
        if (mopTool != null)        mopTool.SetActive(selectedSlot == 1);
        if (bucketTool != null)     bucketTool.SetActive(selectedSlot == 2);
        if (blacklightTool != null) blacklightTool.SetActive(selectedSlot == 3);
        if (sprayTool != null)      sprayTool.SetActive(selectedSlot == 4);
    }

    void ToggleHandTool(int slot, GameObject toolObj)
    {
        if (selectedSlot == slot)
        {
            selectedSlot = -1;
            animationDriver?.ClearSelectedItem();
        }
        else
        {
            if (toolObj && holdPoint != null)
            {
                toolObj.transform.SetParent(holdPoint, false);
                toolObj.transform.localPosition = Vector3.zero;
            }

            selectedSlot = slot;

            if (slot == 1) animationDriver?.SelectMop();
            else           animationDriver?.SelectTool();
        }
    }

    void ClearSelection()
    {
        selectedSlot = -1;

        if (mopTool != null)        mopTool.SetActive(false);
        if (bucketTool != null)     bucketTool.SetActive(false);
        if (blacklightTool != null) blacklightTool.SetActive(false);
        if (sprayTool != null)      sprayTool.SetActive(false);
    }

    public int GetSelectedSlot()    => selectedSlot;
    public bool IsMopSelected()     => selectedSlot == 1;
    public bool IsBucketSelected()  => selectedSlot == 2;
    public bool IsBlacklightSelected() => selectedSlot == 3;
    public bool IsSpraySelected()   => selectedSlot == 4;

}