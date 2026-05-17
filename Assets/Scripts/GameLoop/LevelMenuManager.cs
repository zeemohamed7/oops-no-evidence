using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// LevelMenuManager  —  the in-van briefing panel on the Overworld Map
// ─────────────────────────────────────────────────────────────────────────────
// GDD flow after clicking a level tile:
//   Level Menu (Play / Instructions / Exit)
//     → Play  → Storyline / Big Boss dialogue → load gameplay scene
//     → Instructions → show objective text panel
//     → Exit  → close panel, return to map
//
// Attach this to the Level Menu panel GameObject.
// The panel starts inactive; OverworldMapManager calls Open() to show it.
// ─────────────────────────────────────────────────────────────────────────────
public class LevelMenuManager : MonoBehaviour
{
    // ── inspector ─────────────────────────────────────────────────────────────

    [Header("Level Menu panel elements")]
    public TextMeshProUGUI levelNameText;
    public Button          playButton;
    public Button          instructionsButton;
    public Button          exitButton;

    [Header("Instructions sub-panel")]
    public GameObject      instructionsPanel;
    public TextMeshProUGUI instructionsBodyText;
    public Button          instructionsCloseButton;

    [Header("Storyline / briefing panel")]
    public GameObject      storylinePanel;
    public TextMeshProUGUI bigBossDialogueText;
    [Tooltip("How long the Big Boss dialogue is shown before the level loads.")]
    public float           storylineDisplaySeconds = 4f;
    public Button          storylineSkipButton;    // optional skip button

    // ── runtime ───────────────────────────────────────────────────────────────

    private OverworldMapManager.LevelTileData currentLevel;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlay);
        instructionsButton.onClick.AddListener(OnInstructions);
        exitButton.onClick.AddListener(OnExit);

        if (instructionsCloseButton != null)
            instructionsCloseButton.onClick.AddListener(() => instructionsPanel.SetActive(false));

        if (storylineSkipButton != null)
            storylineSkipButton.onClick.AddListener(OnSkipStoryline);

        gameObject.SetActive(false);
        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (storylinePanel    != null) storylinePanel.SetActive(false);
    }

    // ── public API ────────────────────────────────────────────────────────────

    /// <summary>Called by OverworldMapManager when a tile is clicked.</summary>
    public void Open(OverworldMapManager.LevelTileData level)
    {
        currentLevel = level;
        gameObject.SetActive(true);

        if (levelNameText != null)
            levelNameText.text = level.levelName;

        if (instructionsPanel != null) instructionsPanel.SetActive(false);
        if (storylinePanel    != null) storylinePanel.SetActive(false);
    }

    // ── button handlers ───────────────────────────────────────────────────────

    private void OnPlay()
    {
        playButton.gameObject.SetActive(false);
        instructionsButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        if (storylinePanel != null)
        {
            storylinePanel.SetActive(true);
            if (bigBossDialogueText != null)
                bigBossDialogueText.text = currentLevel.bigBossDialogue;
        }

        // Go to intro scene if defined, otherwise straight to gameplay
        string target = !string.IsNullOrEmpty(currentLevel.introSceneName)
                        ? currentLevel.introSceneName
                        : currentLevel.sceneName;

        StartCoroutine(LoadAfterDelay(target, storylineDisplaySeconds));
    }

    private void OnInstructions()
    {
        if (instructionsPanel == null) return;
        instructionsPanel.SetActive(true);
        if (instructionsBodyText != null)
            instructionsBodyText.text = currentLevel.instructionsText;
    }

    private void OnExit()
    {
        gameObject.SetActive(false);
        // Restore buttons in case they were hidden by OnPlay.
        playButton.gameObject.SetActive(true);
        instructionsButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
    }

    private void OnSkipStoryline()
    {
        StopAllCoroutines();
        string target = !string.IsNullOrEmpty(currentLevel.introSceneName)
                        ? currentLevel.introSceneName
                        : currentLevel.sceneName;
        SceneManager.LoadScene(target);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private IEnumerator LoadAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}