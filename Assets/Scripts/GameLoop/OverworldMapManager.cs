using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// OverworldMapManager  —  level-selection hub
// ─────────────────────────────────────────────────────────────────────────────
// GDD: after the lobby, players land here and pick one of the 5 level tiles.
// Levels unlock linearly (beat Level 1 → Level 2 unlocks, etc.).
// Each tile shows a star rating based on the best grade achieved.
//
// ── Inspector wiring ──────────────────────────────────────────────────────────
// • levels         : 5 LevelTileData entries (one per level in order)
// • levelMenuPanel : the pop-up that appears when a tile is clicked
//                    (LevelMenuManager is on the same panel GameObject)
// • quitButton     : exits to lobby or quits application
// ─────────────────────────────────────────────────────────────────────────────
public class OverworldMapManager : MonoBehaviour
{
    // ── data ──────────────────────────────────────────────────────────────────

    [System.Serializable]
    public class LevelTileData
    {
        public string        levelName;           // e.g. "The Late-Night Shawarma Shop"
        public string        sceneName;           // Unity build scene name
        public Button        tileButton;          // the clickable tile on the map
        public Image         lockIcon;            // shown when level is locked
        public Image[]       starImages;          // 3 star Image components (fill = gold/grey)
        [TextArea(2, 4)]
        public string        instructionsText;    // shown in the Level Menu instructions panel
        [TextArea(1, 2)]
        public string        bigBossDialogue;     // intro briefing line
        [Tooltip("Storyline/intro scene to load before gameplay. Leave empty to skip.")]
        public string        introSceneName;
    }

    [Header("Level Tiles (5 in order)")]
    public LevelTileData[] levels;

    [Header("Level Menu Panel")]
    public LevelMenuManager levelMenuPanel;

    [Header("Quit Button")]
    public Button quitButton;

    [Header("Colors")]
    public Color starFilledColor = new Color(1f, 0.85f, 0f);   // gold
    public Color starEmptyColor  = new Color(0.3f, 0.3f, 0.3f); // grey

    // PlayerPrefs keys
    private const string UnlockedKey = "LevelUnlocked_";
    private const string StarsKey    = "LevelStars_";

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Level 1 is always unlocked.
        if (!PlayerPrefs.HasKey(UnlockedKey + 0))
            PlayerPrefs.SetInt(UnlockedKey + 0, 1);

        for (int i = 0; i < levels.Length; i++)
        {
            int index = i; // capture for lambda
            levels[i].tileButton.onClick.AddListener(() => OnTileClicked(index));
        }

        if (quitButton != null)
            quitButton.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("VanLobby"));

        RefreshAllTiles();
    }

    // ── public API (called by LevelMenuManager after a level completes) ───────

    /// <summary>
    /// Call this from GameManager or the result screen to record the grade.
    /// grade: "S"=5, "A"=4, "B"=3, "C"=2, "D"=1, "F"=0
    /// </summary>
    public static void RecordLevelComplete(int levelIndex, string grade)
    {
        int stars = GradeToStars(grade);
        string starsKey    = StarsKey    + levelIndex;
        string unlockedKey = UnlockedKey + (levelIndex + 1);

        // Only update if new score is better.
        int prev = PlayerPrefs.GetInt(starsKey, 0);
        if (stars > prev) PlayerPrefs.SetInt(starsKey, stars);

        // Unlock the next level.
        PlayerPrefs.SetInt(unlockedKey, 1);
        PlayerPrefs.Save();
    }

    // ── internals ─────────────────────────────────────────────────────────────

    private void OnTileClicked(int index)
    {
        bool unlocked = PlayerPrefs.GetInt(UnlockedKey + index, 0) == 1;
        if (!unlocked) return;

        if (LobbyManager.Instance != null)
            LobbyManager.Instance.SetCurrentLevel(index);

        levelMenuPanel.Open(levels[index]);
    }

    private void RefreshAllTiles()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            bool unlocked = PlayerPrefs.GetInt(UnlockedKey + i, 0) == 1;
            int  stars    = PlayerPrefs.GetInt(StarsKey    + i, 0);

            // Lock icon
            if (levels[i].lockIcon != null)
                levels[i].lockIcon.enabled = !unlocked;

            levels[i].tileButton.interactable = unlocked;

            // Star icons
            if (levels[i].starImages != null)
            {
                for (int s = 0; s < levels[i].starImages.Length; s++)
                {
                    if (levels[i].starImages[s] == null) continue;
                    levels[i].starImages[s].color = (s < stars) ? starFilledColor : starEmptyColor;
                }
            }
        }
    }

    private static int GradeToStars(string grade)
    {
        switch (grade)
        {
            case "S": case "A": return 3;
            case "B":           return 2;
            case "C": case "D": return 1;
            default:            return 0; // F
        }
    }
}