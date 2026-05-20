using UnityEngine;
using UnityEngine.UI;

// Attach to InventoryPanel. Assign inventoryPanel (direct parent of the 4 slot Images).
// Players are found automatically at runtime — works from lobby or directly from scene.
public class InventoryHUD : MonoBehaviour
{
    [Header("Shared Panel — direct parent of the 4 slot Images")]
    public Transform inventoryPanel;

    private Image[]      _slotImages;
    private Coroutine[]  _coroutines = new Coroutine[4]; // one per slot
    private ToolInventory[] players;
    private int[]        _prevSlots; // tracks last known slot per player

    private static readonly Color Lavender = new Color(0.72f, 0.52f, 1f, 1f);
    private static readonly Color Normal   = Color.white;
    private static readonly Color Locked   = new Color(0.35f, 0.35f, 0.35f, 0.5f);
    private const float FlashDuration = 0.5f;

    void Start()
    {
        if (inventoryPanel != null)
        {
            _slotImages = new Image[inventoryPanel.childCount];
            for (int i = 0; i < inventoryPanel.childCount; i++)
            {
                var child = inventoryPanel.GetChild(i);
                child.gameObject.SetActive(true);
                _slotImages[i] = child.GetComponent<Image>();
            }
        }
    }

    void Update()
    {
        // Lazy-find players — works whether spawned by lobby or pre-placed in scene
        if (players == null || players.Length == 0)
        {
            players = FindObjectsByType<ToolInventory>(FindObjectsSortMode.None);
            if (players.Length > 0)
                _prevSlots = new int[players.Length]; // initialises to 0, treated as -1 below
        }

        if (players == null || players.Length == 0) return;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            int current = players[i].GetSelectedSlot();
            int prev    = _prevSlots[i] - 1; // stored as slot+1 so 0 means "unset"

            if (current != prev)
            {
                if (current >= 1 && current <= 4)
                    Flash(current);
                _prevSlots[i] = current + 1; // store as slot+1
            }
        }

        // Grey out locked slots every frame so it always overrides highlights
        bool blacklightLocked = players.Length > 0 && !players[0].IsBlacklightUnlocked();
        Image slot3Img = SlotImage(3);
        if (slot3Img != null)
        {
            if (blacklightLocked)
            {
                // Cancel any running flash and force grey
                if (_coroutines[2] != null) { StopCoroutine(_coroutines[2]); _coroutines[2] = null; }
                slot3Img.color = Locked;
            }
        }
    }

    void Flash(int slot)
    {
        Image img = SlotImage(slot);
        if (img == null) return;

        int idx = slot - 1;
        if (_coroutines[idx] != null) StopCoroutine(_coroutines[idx]);
        _coroutines[idx] = StartCoroutine(DoFlash(img));
    }

    System.Collections.IEnumerator DoFlash(Image img)
    {
        img.color = Lavender;
        float t = 0f;
        while (t < FlashDuration)
        {
            t += Time.deltaTime;
            img.color = Color.Lerp(Lavender, Normal, t / FlashDuration);
            yield return null;
        }
        img.color = Normal;
    }

    Image SlotImage(int slot)
    {
        if (_slotImages == null || slot < 1 || slot > _slotImages.Length) return null;
        return _slotImages[slot - 1];
    }
}