using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// ─────────────────────────────────────────────────────────────────────────────
// LobbyManager  —  Van Waiting Lobby scene
// ─────────────────────────────────────────────────────────────────────────────
// Attach to an empty GameObject in the VanLobby scene.
// Wire 4 PlayerSlotUI components (each on its own panel) + the Start/Quit buttons.
// ─────────────────────────────────────────────────────────────────────────────
public class LobbyManager : MonoBehaviour
{
    [Header("Player Slot Panels (assign 4)")]
    public PlayerSlotUI[] playerSlots;

    [Header("Buttons")]
    public Button startButton;
    public Button quitButton;

    [Header("Character Previews (optional — one Sprite per character index)")]
    public Sprite[] characterPreviews;

    [Header("Scene Names")]
    public string overworldSceneName = "OverworldMap";

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
        int joined = 0;
        int ready  = 0;
        foreach (var slot in playerSlots)
        {
            if (slot.State == PlayerSlotUI.SlotState.Ready)         { joined++; ready++; }
            else if (slot.State == PlayerSlotUI.SlotState.Selecting)  joined++;
        }
        startButton.interactable = (joined > 0 && joined == ready);
    }

    public void OnSlotChanged() { /* Update loop handles the button state. */ }

    private void OnStartPressed()
    {
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
