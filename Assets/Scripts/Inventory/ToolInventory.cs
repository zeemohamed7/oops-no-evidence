using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    
   //  Track if a heavy item is overriding the hand tools
    public bool isCarryingHeavyObject = false;

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

    // Blacklight is only available in Level 3 and Level 4
    bool BlacklightUnlocked()
    {
        string scene = SceneManager.GetActiveScene().name;
        return scene.Contains("Level3") || scene.Contains("Level4");
    }

    void Update()
    {
        // Stop all tool swapping and handling inside the lobby
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            ClearSelection(); // Keeps all tools hidden on character select
            return;
        }

        HandleInput();
        EnforceSingleTool();
    }

    void HandleInput()
    {
        if (_slot1 != null && _slot1.WasPressedThisFrame()) ToggleHandTool(1, mopTool);
        if (_slot2 != null && _slot2.WasPressedThisFrame()) ToggleHandTool(2, bucketTool);
        if (_slot3 != null && _slot3.WasPressedThisFrame()) ToggleHandTool(3, sprayTool);
        if (_slot4 != null && _slot4.WasPressedThisFrame() && BlacklightUnlocked()) ToggleHandTool(4, blacklightTool);

        if (_previous != null && _previous.WasPressedThisFrame()) CycleSlot(-1);
        if (_next     != null && _next.WasPressedThisFrame())     CycleSlot(+1);
    }

    void CycleSlot(int dir)
    {
        // 1. Calculate next slot
        int next = selectedSlot == -1 ? (dir > 0 ? 1 : 4) : selectedSlot + dir;

        // 2. Wrap around
        if (next < 1) next = 4;
        if (next > 4) next = 1;

        // 3. Skip slot 4 if blacklight is locked
        if (next == 4 && !BlacklightUnlocked())
        {
            // If dir is positive, go to 1. If negative, go to 3.
            next = (dir > 0) ? 1 : 3;
        }

        selectedSlot = next;
        
        // Ensure the tool animation and parenting updates when cycling
        UpdateToolState();
    }

    // Call this after cycling to update animations/parents immediately
    void UpdateToolState()
    {
        GameObject activeTool = null;
        if (selectedSlot == 1) activeTool = mopTool;
        else if (selectedSlot == 2) activeTool = bucketTool;
        else if (selectedSlot == 3) activeTool = sprayTool;
        else if (selectedSlot == 4) activeTool = blacklightTool;

        if (activeTool != null && holdPoint != null)
        {
            activeTool.transform.SetParent(holdPoint, false);
            activeTool.transform.localPosition = Vector3.zero;
        }

        if (selectedSlot == 1) animationDriver?.SelectMop();
        else if (selectedSlot != -1) animationDriver?.SelectTool();
        else animationDriver?.ClearSelectedItem();
    }

    // Runs every frame — keeps tool visibility in sync with selectedSlot
    void EnforceSingleTool()
    {
        
        // Turn all tools off if carrying a body or weapon
        if (isCarryingHeavyObject)
        {
            if (mopTool != null)        mopTool.SetActive(false);
            if (bucketTool != null)     bucketTool.SetActive(false);
            if (blacklightTool != null) blacklightTool.SetActive(false);
            if (sprayTool != null)      sprayTool.SetActive(false);
            return; 
        }
        
        // If blacklight somehow got selected while locked, deselect it
        if (selectedSlot == 4 && !BlacklightUnlocked())
            selectedSlot = -1;

        if (mopTool != null)        mopTool.SetActive(selectedSlot == 1);
        if (bucketTool != null)     bucketTool.SetActive(selectedSlot == 2);
        if (sprayTool != null)      sprayTool.SetActive(selectedSlot == 3);
        if (blacklightTool != null) blacklightTool.SetActive(selectedSlot == 4);
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

    public int GetSelectedSlot()       => selectedSlot;
    public bool IsMopSelected()        => selectedSlot == 1;
    public bool IsBucketSelected()     => selectedSlot == 2;
    public bool IsSpraySelected()      => selectedSlot == 3;
    public bool IsBlacklightSelected() => selectedSlot == 4;
    public bool IsBlacklightUnlocked() => BlacklightUnlocked();
}