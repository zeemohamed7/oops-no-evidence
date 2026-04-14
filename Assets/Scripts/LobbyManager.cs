using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// LobbyManager  —  Van Waiting Lobby scene
// ─────────────────────────────────────────────────────────────────────────────
// GDD flow:
//   Splash → Van Waiting Lobby → Character Select → Ready → Overworld Map
//
// Each of the 4 PlayerSlot panels represents one possible player.
// A slot starts EMPTY. Clicking its Join button moves it to SELECTING (the
// player picks a character), then READY when they confirm.
// Once at least one slot is READY and every joined slot is READY, the
// Start button becomes interactable and loads the Overworld Map.
//
// ── Inspector wiring ──────────────────────────────────────────────────────────
// • playerSlots  : drag in 4 PlayerSlotUI references (one per UI panel)
// • startButton  : the "READY / START" button at the bottom
// • quitButton   : exits the application
// • characterPreviews : optional array of Sprites (one per character) shown in
//                       the preview panel — leave empty if you have no sprites yet
// ─────────────────────────────────────────────────────────────────────────────
public class LobbyManager : MonoBehaviour
{
    // ── serialised ────────────────────────────────────────────────────────────

    [Header("Player Slot Panels (assign 4)")]
    public PlayerSlotUI[] playerSlots;

    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Character Previews (optional, one Sprite per character)")]
    public Sprite[] characterPreviews; // index matches LobbyData.CharacterNames

    [Header("Scene Names")]
    public string overworldSceneName = "OverworldMap";

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        LobbyData.Reset();

        startButton.onClick.AddListener(OnStartPressed);
        startButton.interactable = false;

        if (quitButton != null)
            quitButton.onClick.AddListener(Application.Quit);

        foreach (var slot in playerSlots)
            slot.Initialise(this, characterPreviews);
    }

    private void Update()
    {
        // Enable Start only when ≥1 player has joined and all joined are ready.
        int joined = 0;
        int ready  = 0;
        foreach (var slot in playerSlots)
        {
            if (slot.State == PlayerSlotUI.SlotState.Ready)    { joined++; ready++; }
            else if (slot.State == PlayerSlotUI.SlotState.Selecting) joined++;
        }
        startButton.interactable = (joined > 0 && joined == ready);
    }

    // ── called by slots ───────────────────────────────────────────────────────

    public void OnSlotChanged() { /* Update loop handles the button. */ }

    // ── buttons ───────────────────────────────────────────────────────────────

    private void OnStartPressed()
    {
        // Write final selections into LobbyData before leaving.
        LobbyData.PlayerCount = 0;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].State == PlayerSlotUI.SlotState.Ready)
            {
                LobbyData.PlayerCharacters[LobbyData.PlayerCount] = playerSlots[i].SelectedCharacter;
                LobbyData.PlayerCount++;
            }
        }
        SceneManager.LoadScene(overworldSceneName);
    }
}


// ─────────────────────────────────────────────────────────────────────────────
// PlayerSlotUI  —  one of the four slots shown in the lobby
// ─────────────────────────────────────────────────────────────────────────────
// Attach this to each Player Slot panel GameObject in the scene.
// Wire up the child UI references via the inspector.
//
// Slot lifecycle:  Empty → Selecting → Ready
//                  Ready → Selecting  (un-ready)
//                  Selecting → Empty  (leave)
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public class PlayerSlotUI : MonoBehaviour
{
    public enum SlotState { Empty, Selecting, Ready }

    // ── inspector ─────────────────────────────────────────────────────────────

    [Header("State panels")]
    public GameObject emptyPanel;       // "Press Join" prompt
    public GameObject selectingPanel;   // character carousel + leave/ready buttons
    public GameObject readyPanel;       // "READY" confirmation panel (optional overlay)

    [Header("Selecting panel children")]
    public TextMeshProUGUI characterNameText;
    public Image           characterPreviewImage;
    public Button          prevCharButton;
    public Button          nextCharButton;
    public Button          readyButton;
    public Button          leaveButton;

    [Header("Empty panel children")]
    public Button joinButton;

    [Header("Ready panel children (optional)")]
    public TextMeshProUGUI readyCharacterNameText;

    // ── runtime ───────────────────────────────────────────────────────────────

    public SlotState State           { get; private set; } = SlotState.Empty;
    public int       SelectedCharacter { get; private set; } = 0; // index into LobbyData.CharacterNames

    private LobbyManager manager;
    private Sprite[]      previews;

    // ── init ─────────────────────────────────────────────────────────────────

    public void Initialise(LobbyManager mgr, Sprite[] characterSprites)
    {
        manager  = mgr;
        previews = characterSprites;

        joinButton.onClick.AddListener(OnJoin);
        prevCharButton.onClick.AddListener(OnPrev);
        nextCharButton.onClick.AddListener(OnNext);
        readyButton.onClick.AddListener(OnReady);
        leaveButton.onClick.AddListener(OnLeave);

        SetState(SlotState.Empty);
    }

    // ── button handlers ───────────────────────────────────────────────────────

    private void OnJoin()
    {
        // Assign the default character (index 0) to start.
        SelectedCharacter = 0;
        SetState(SlotState.Selecting);
    }

    private void OnPrev()
    {
        // Cycle backward through characters, skipping ones taken by other slots.
        int next = SelectedCharacter;
        for (int attempt = 0; attempt < LobbyData.CharacterCount; attempt++)
        {
            next = (next - 1 + LobbyData.CharacterCount) % LobbyData.CharacterCount;
            if (!IsTakenByOther(next)) { SelectedCharacter = next; break; }
        }
        RefreshSelectingPanel();
        manager.OnSlotChanged();
    }

    private void OnNext()
    {
        int next = SelectedCharacter;
        for (int attempt = 0; attempt < LobbyData.CharacterCount; attempt++)
        {
            next = (next + 1) % LobbyData.CharacterCount;
            if (!IsTakenByOther(next)) { SelectedCharacter = next; break; }
        }
        RefreshSelectingPanel();
        manager.OnSlotChanged();
    }

    private void OnReady()
    {
        SetState(SlotState.Ready);
    }

    private void OnLeave()
    {
        SetState(SlotState.Empty);
    }

    // ── state machine ─────────────────────────────────────────────────────────

    private void SetState(SlotState newState)
    {
        State = newState;

        emptyPanel.SetActive(newState == SlotState.Empty);
        selectingPanel.SetActive(newState == SlotState.Selecting);

        if (readyPanel != null)
            readyPanel.SetActive(newState == SlotState.Ready);

        if (newState == SlotState.Selecting)
            RefreshSelectingPanel();

        if (newState == SlotState.Ready && readyCharacterNameText != null)
            readyCharacterNameText.text = LobbyData.CharacterNames[SelectedCharacter];

        manager.OnSlotChanged();
    }

    private void RefreshSelectingPanel()
    {
        if (characterNameText != null)
            characterNameText.text = LobbyData.CharacterNames[SelectedCharacter];

        if (characterPreviewImage != null && previews != null &&
            SelectedCharacter < previews.Length && previews[SelectedCharacter] != null)
        {
            characterPreviewImage.sprite  = previews[SelectedCharacter];
            characterPreviewImage.enabled = true;
        }
        else if (characterPreviewImage != null)
        {
            characterPreviewImage.enabled = false;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    // Returns true if another slot (not this one) has already claimed charIndex.
    private bool IsTakenByOther(int charIndex)
    {
        var allSlots = manager.playerSlots;
        foreach (var slot in allSlots)
        {
            if (slot == this) continue;
            if (slot.State != SlotState.Empty && slot.SelectedCharacter == charIndex)
                return true;
        }
        return false;
    }
}
