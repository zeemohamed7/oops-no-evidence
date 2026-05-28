using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Attach to: manager-win-lose
// Inspector wiring:
//   - winTitle        → win-title GameObject
//   - loseTitle       → lose-title GameObject
//   - taskRows        → task-1 … task-N TextMeshProUGUI objects (any count)
//   - taskCountText   → the "0/3" counter TMP
//   - listText        → failure reasons TMP (optional, separate from task rows)
//   - susText         → sus percentage TMP
//   - susFillImage    → Image (Filled, Horizontal) for the sus bar
//   - timeText        → time remaining TMP
//   - gradeS/A/B/C/D/F → child GameObjects under grades
//   - retryButton / nextLevelButton / quitButton → Button components
public class WinLoseScreenManager : MonoBehaviour
{
    public static WinLoseScreenManager Instance { get; private set; }
    bool _shown;

    void Awake() { Instance = this; }

    [Header("Panel Root")]
    public GameObject panelRoot;      // drag the WinLoseCanvas here
    public GameObject gamePlayCanvas; // drag the GamePlayCanvas here — hidden on win/loss

    [Header("Title GameObjects")]
    public GameObject winTitle;
    public GameObject loseTitle;

    [Header("Task Panel")]
    public TextMeshProUGUI[] taskRows;       // task-1 … task-N TMPs (supports 4–7+)
    public TextMeshProUGUI taskCountText;    // "0/3" counter TMP
    public Color taskDoneColor = new Color(0.4f, 0.9f, 0.4f);
    public Color taskPendingColor = Color.white;

    [Header("Info Texts")]
    public TextMeshProUGUI listText;   // failure reasons bullet list (optional)
    public TextMeshProUGUI susText;    // suspicion percentage
    public TextMeshProUGUI timeText;   // time remaining mm:ss

    [Header("Sus Fill")]
    public Image susFillImage;         // Image (Filled, Horizontal) for sus bar


    [Header("Grade Letter GameObjects")]
    public GameObject gradeS;
    public GameObject gradeA;
    public GameObject gradeB;
    public GameObject gradeC;
    public GameObject gradeD;

    [Header("Buttons")]
    public Button nextLevelButton;  // shown on win
    public Button retryButton;      // shown on loss
    public Button quitButton;       // home — always visible

    // PlayerPrefs keys — written by SaveResultToPrefs() before loading this scene
    const string KEY_WIN = "Result_IsWin";
    const string KEY_GRADE = "Result_Grade";
    const string KEY_TIME = "Result_TimeRemaining";
    const string KEY_SUS = "Result_Suspicion";      // stored as 0-1 ratio
    const string KEY_FAILURES = "Result_Failures";       // pipe-separated
    const string KEY_TASK_NAMES = "Result_TaskNames";      // pipe-separated task labels
    const string KEY_TASK_DONE = "Result_TaskDone";       // pipe-separated 0/1

    void Start()
    {
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(NextLevel);
        if (retryButton != null) retryButton.onClick.AddListener(RetryLevel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMap);

        // In-scene overlay: already shown by direct call — don't hide it again.
        if (_shown) return;

        if (GameManager.Instance != null)
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
            return;
        }

        // Separate scene: read everything from PlayerPrefs
        PopulateUI(
            isWin: PlayerPrefs.GetInt(KEY_WIN, 0) == 1,
            grade: PlayerPrefs.GetString(KEY_GRADE, "F"),
            timeRemaining: PlayerPrefs.GetFloat(KEY_TIME, 0f),
            suspicion01: PlayerPrefs.GetFloat(KEY_SUS, 0f),
            failures: SplitPipe(PlayerPrefs.GetString(KEY_FAILURES, "")),
            tasks: LoadTasksFromPrefs()
        );
    }

    void ActivateHierarchy()
    {
        // Walk up and enable any inactive parent so the panel actually appears.
        Transform t = transform.parent;
        while (t != null) { t.gameObject.SetActive(true); t = t.parent; }
        gameObject.SetActive(true);
    }

    static float CaptureSus()
    {
        if (SuspicionMeter.Instance == null)
        {
            Debug.LogWarning("WinLoseScreenManager: SuspicionMeter.Instance is null — sus will show 0%");
            return 0f;
        }
        float v = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
        Debug.Log($"[WinLose] sus captured = {v * 100f:0}%");
        return v;
    }

    public void ShowWin(GameManager gm, GameHUD hud)
    {
        _shown = true;
        float sus01 = CaptureSus();                    // read BEFORE hiding anything
        string grade = gm?.CalculateGrade() ?? "S";
        float time = gm?.TimeRemaining ?? 0f;
        var tasks = hud?.GetTaskSnapshot();

        if (gamePlayCanvas != null) gamePlayCanvas.SetActive(false);
        ActivateHierarchy();
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
        PopulateUI(isWin: true, grade: grade, timeRemaining: time,
                   suspicion01: sus01, failures: null, tasks: tasks);
    }

    public void ShowLoss(GameManager gm, GameHUD hud)
    {
        _shown = true;
        float sus01 = CaptureSus();                  // read BEFORE hiding anything
        string grade = gm?.CalculateGrade() ?? "F";
        float time = gm?.TimeRemaining ?? 0f;
        var failures = gm?.LastFailureReasons;
        var tasks = hud?.GetTaskSnapshot();

        if (gamePlayCanvas != null) gamePlayCanvas.SetActive(false);
        ActivateHierarchy();
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
        PopulateUI(isWin: false, grade: grade, timeRemaining: time,
                   suspicion01: sus01, failures: failures, tasks: tasks);
    }

    void PopulateUI(bool isWin, string grade, float timeRemaining, float suspicion01,
                    List<string> failures, (string label, bool done)[] tasks)
    {
        // Titles
        if (winTitle != null) winTitle.SetActive(isWin);
        if (loseTitle != null) loseTitle.SetActive(!isWin);

        // Grade bubble
        ActivateGrade(grade);

        // Time remaining
        if (timeText != null)
        {
            int m = Mathf.FloorToInt(timeRemaining / 60f);
            int s = Mathf.FloorToInt(timeRemaining % 60f);
            timeText.text = $"{m:00}:{s:00}";
        }

        // Suspicion percentage text
        if (susText != null)
            susText.text = $"{Mathf.RoundToInt(suspicion01 * 100f)}%";

        // Suspicion fill bar — force Filled/Horizontal so fillAmount actually clips the image
        if (susFillImage != null)
        {
            susFillImage.type = Image.Type.Filled;
            susFillImage.fillMethod = Image.FillMethod.Horizontal;
            susFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            susFillImage.fillAmount = Mathf.Clamp01(suspicion01);
        }

        // Task rows — show only as many as the level has, hide the rest
        PopulateTaskRows(tasks);

        // Failure reasons list (optional separate display)
        if (listText != null)
        {
            bool show = !isWin && failures != null && failures.Count > 0;
            listText.gameObject.SetActive(show);
            if (show)
                listText.text = "• " + string.Join("\n• ", failures);
        }

        // Next level on win, retry on loss; home always visible
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(isWin);
        if (retryButton != null) retryButton.gameObject.SetActive(!isWin);
    }

    void PopulateTaskRows((string label, bool done)[] tasks)
    {
        if (taskRows == null || taskRows.Length == 0) return;

        int taskCount = tasks != null ? tasks.Length : 0;
        int doneCount = 0;

        for (int i = 0; i < taskRows.Length; i++)
        {
            if (taskRows[i] == null) continue;

            if (tasks != null && i < tasks.Length)
            {
                taskRows[i].gameObject.SetActive(true);
                taskRows[i].color = tasks[i].done ? taskDoneColor : taskPendingColor;
                taskRows[i].text = tasks[i].done
                    ? $"<s>{tasks[i].label}</s>"
                    : tasks[i].label;
                if (tasks[i].done) doneCount++;
            }
            else
            {
                // This level has fewer tasks than rows wired — hide the extra row
                taskRows[i].gameObject.SetActive(false);
            }
        }

        if (taskCountText != null)
            taskCountText.text = $"{doneCount}/{taskCount}";
    }

    void ActivateGrade(string grade)
    {
        // Clamp F down to D since there is no F bubble
        if (grade == "F") grade = "D";

        var map = new (string key, GameObject obj)[]
        {
            ("S", gradeS), ("A", gradeA), ("B", gradeB),
            ("C", gradeC), ("D", gradeD)
        };
        foreach (var (key, obj) in map)
            if (obj != null) obj.SetActive(key == grade);
    }

    float GetSuspicion01()
    {
        if (SuspicionMeter.Instance == null) return 0f;
        return SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
    }

    // ── PlayerPrefs helpers ──────────────────────────────────────────────────

    static List<string> SplitPipe(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return new List<string>();
        return new List<string>(raw.Split('|'));
    }

    static (string label, bool done)[] LoadTasksFromPrefs()
    {
        string namesRaw = PlayerPrefs.GetString(KEY_TASK_NAMES, "");
        string doneRaw = PlayerPrefs.GetString(KEY_TASK_DONE, "");
        if (string.IsNullOrEmpty(namesRaw)) return null;

        string[] names = namesRaw.Split('|');
        string[] dones = doneRaw.Split('|');
        var result = new (string, bool)[names.Length];
        for (int i = 0; i < names.Length; i++)
            result[i] = (names[i], i < dones.Length && dones[i] == "1");
        return result;
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    void NextLevel()
    {
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.SwitchToScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    void RetryLevel()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 🟢 FIXED: Use the SceneTransitionManager to reload the current level smoothly
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.SwitchToScene(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
    }

    void QuitToMap()
    {
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.SwitchToScene("Lobby");
        }
        else
        {
            SceneManager.LoadScene("Lobby");
        }
    }

    // ── Call this before loading the win-lose scene (separate-scene flow) ────
    // Example:
    //   var tasks = GameHUD.Instance?.GetTaskSnapshot();
    //   WinLoseScreenManager.SaveResultToPrefs(true, "A", 120f, 0.2f, null, tasks);
    //   SceneManager.LoadScene("win-lose");
    public static void SaveResultToPrefs(bool isWin, string grade, float timeRemaining,
                                         float suspicion01, List<string> failures,
                                         (string label, bool done)[] tasks)
    {
        PlayerPrefs.SetInt(KEY_WIN, isWin ? 1 : 0);
        PlayerPrefs.SetString(KEY_GRADE, grade);
        PlayerPrefs.SetFloat(KEY_TIME, timeRemaining);
        PlayerPrefs.SetFloat(KEY_SUS, suspicion01);
        PlayerPrefs.SetString(KEY_FAILURES, failures != null ? string.Join("|", failures) : "");

        if (tasks != null && tasks.Length > 0)
        {
            var names = new string[tasks.Length];
            var dones = new string[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                names[i] = tasks[i].label;
                dones[i] = tasks[i].done ? "1" : "0";
            }
            PlayerPrefs.SetString(KEY_TASK_NAMES, string.Join("|", names));
            PlayerPrefs.SetString(KEY_TASK_DONE, string.Join("|", dones));
        }
        else
        {
            PlayerPrefs.SetString(KEY_TASK_NAMES, "");
            PlayerPrefs.SetString(KEY_TASK_DONE, "");
        }

        PlayerPrefs.Save();
    }
}