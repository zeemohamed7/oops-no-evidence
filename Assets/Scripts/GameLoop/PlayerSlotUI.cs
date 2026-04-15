using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// PlayerSlotUI  —  one of the four player slots in the Van Waiting Lobby
// ─────────────────────────────────────────────────────────────────────────────
// Attach this to each Player Slot panel GameObject.
// Wire up all child UI references via the Inspector.
//
// Slot lifecycle:  Empty → Selecting → Ready
//                  Ready  → Selecting  (un-ready)
//                  Selecting → Empty   (leave)
// ─────────────────────────────────────────────────────────────────────────────
public class PlayerSlotUI : MonoBehaviour
{
    public enum SlotState { Empty, Selecting, Ready }

    // ── inspector ─────────────────────────────────────────────────────────────

    [Header("State panels")]
    public GameObject emptyPanel;       // "Press Join" prompt
    public GameObject selectingPanel;   // character carousel + leave/ready buttons
    public GameObject readyPanel;       // "READY" overlay (optional)

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

    public SlotState State              { get; private set; } = SlotState.Empty;
    public int       SelectedCharacter  { get; private set; } = 0;

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
        SelectedCharacter = 0;
        SetState(SlotState.Selecting);
    }

    private void OnPrev()
    {
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

    private void OnReady()    => SetState(SlotState.Ready);
    private void OnLeave()    => SetState(SlotState.Empty);

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

        if (characterPreviewImage != null)
        {
            bool hasSprite = previews != null &&
                             SelectedCharacter < previews.Length &&
                             previews[SelectedCharacter] != null;
            characterPreviewImage.enabled = hasSprite;
            if (hasSprite) characterPreviewImage.sprite = previews[SelectedCharacter];
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private bool IsTakenByOther(int charIndex)
    {
        foreach (var slot in manager.playerSlots)
        {
            if (slot == this) continue;
            if (slot.State != SlotState.Empty && slot.SelectedCharacter == charIndex)
                return true;
        }
        return false;
    }
}
