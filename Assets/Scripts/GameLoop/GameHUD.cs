using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance;
    private void Awake() { Instance = this; }

    // ── TIMER ─────────────────────────────────────────────────────────────
    [Header("Timer  (TimePanel > timeText)")]
    public TextMeshProUGUI timeText;
    public float levelDurationOverride = 180f; // 3 min for level 1 — set per level in Inspector

    [Header("Timer Warning Colors")]
    public Color timerNormal   = Color.white;
    public Color timerWarning  = new Color(1f, 0.85f, 0f);   // yellow  < 60s
    public Color timerCritical = new Color(0.9f, 0.1f, 0.1f); // red     < 30s

    // ── SUSPICION BAR ──────────────────────────────────────────────────────
    [Header("Suspicion Bar  (susPanel children)")]
    public Image susFill;               // susPanel > fill
    public TextMeshProUGUI susText;     // susPanel > text
    float susVisual;
    int susStage = -1;

    [Header("Sus Bar Colors  (fill image color)")]
    public Color colorCalm       = new Color(0.3f, 0.85f, 0.3f);  // green
    public Color colorSuspicious = new Color(1f,   0.85f, 0f);    // yellow
    public Color colorAlert      = new Color(1f,   0.5f,  0f);    // orange
    public Color colorPanic      = new Color(0.9f, 0.1f,  0.1f);  // red

    [Header("Sus Status Messages")]
    public string[] calmMsgs  = { "All quiet...", "Keep it clean." };
    public string[] midMsgs   = { "They're looking!", "Watch out!" };
    public string[] highMsgs  = { "GET OUT!", "THEY KNOW!" };

    // ── TASKS ──────────────────────────────────────────────────────────────
    [Header("Tasks  (TaskPanel children)")]
    public TextMeshProUGUI[] taskTexts;   // drag the 3 task TMP objects here
    public TextMeshProUGUI counterText;   // TaskPanel > counter
    public Color taskDoneColor = new Color(0.4f, 0.9f, 0.4f);
    bool[] taskDone;

    // ── PAUSE ──────────────────────────────────────────────────────────────
    [Header("Pause")]
    public Button pauseButton;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitToMapButton;
    bool isPaused;

    // ── RESULT SCREEN ──────────────────────────────────────────────────────
    [Header("Result Screen")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultHeader;        // WIN / LOSE
    public TextMeshProUGUI gradeText;           // S / F
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI failureReasonsText;  // bullet list of failure reasons (loss only)
    public Button nextLevelButton;
    public Button retryButton;
    public Button quitResultButton;

    [Header("Result Colors")]
    public Color winColor  = new Color(0.2f, 0.8f, 0.3f);
    public Color loseColor = new Color(0.9f, 0.2f, 0.2f);

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Override GameManager duration for this level
        if (GameManager.Instance != null)
            GameManager.Instance.levelDuration = levelDurationOverride;

        // Tasks
        taskDone = new bool[taskTexts.Length];
        RefreshCounter();

        // Panels off
        if (pausePanel  != null) pausePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // Buttons
        if (pauseButton     != null) pauseButton.onClick.AddListener(TogglePause);
        if (resumeButton    != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton   != null) restartButton.onClick.AddListener(RestartLevel);
        if (quitToMapButton != null) quitToMapButton.onClick.AddListener(QuitToMap);
        if (quitResultButton!= null) quitResultButton.onClick.AddListener(QuitToMap);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(LoadNextLevel);
        if (retryButton     != null) retryButton.onClick.AddListener(RestartLevel);

        // Game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWin.AddListener(ShowWinScreen);
            GameManager.Instance.OnLoss.AddListener(ShowLossScreen);
        }

        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.OnStateChangedEvent.AddListener(OnSusStateChanged);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (GameManager.Instance == null) return;

        UpdateTimer();
        UpdateSusBar();
    }

    // ── Timer ─────────────────────────────────────────────────────────────

    void UpdateTimer()
    {
        if (timeText == null) return;
        float t = GameManager.Instance.TimeRemaining;
        timeText.text  = GameManager.Instance.FormatTime(t);
        timeText.color = t <= 30f ? timerCritical : t <= 60f ? timerWarning : timerNormal;
    }

    // ── Suspicion ─────────────────────────────────────────────────────────

    void UpdateSusBar()
    {
        if (susFill == null || SuspicionMeter.Instance == null) return;

        float target = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
        susVisual = Mathf.Lerp(susVisual, target, Time.deltaTime * 6f);

        // Requires: susFill Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left
        susFill.fillAmount = susVisual;

        // Status text
        if (susText != null)
        {
            int stage = susVisual < 0.33f ? 0 : susVisual < 0.66f ? 1 : 2;
            if (stage != susStage)
            {
                susStage = stage;
                string[] pool = stage == 0 ? calmMsgs : stage == 1 ? midMsgs : highMsgs;
                susText.text = pool[Random.Range(0, pool.Length)];
            }
        }
    }

    void OnSusStateChanged(SuspicionMeter.SuspicionState state)
    {
        if (susFill == null) return;
        susFill.color = state switch
        {
            SuspicionMeter.SuspicionState.Calm       => colorCalm,
            SuspicionMeter.SuspicionState.Suspicious => colorSuspicious,
            SuspicionMeter.SuspicionState.Alert      => colorAlert,
            SuspicionMeter.SuspicionState.Panic      => colorPanic,
            _ => colorCalm
        };
    }

    // ── Tasks ─────────────────────────────────────────────────────────────

    // Call from gameplay scripts:  GameHUD.Instance.CompleteTask(0);  (0, 1, or 2)
    public void CompleteTask(int index)
    {
        if (index < 0 || index >= taskTexts.Length) return;
        if (taskDone[index]) return;

        taskDone[index] = true;

        if (taskTexts[index] != null)
        {
            taskTexts[index].color = taskDoneColor;
            StartCoroutine(BounceText(taskTexts[index].transform));
        }

        RefreshCounter();

        if (AllTasksDone())
            Debug.Log("All tasks done — head to the van!");
    }

    void RefreshCounter()
    {
        if (counterText == null) return;
        int done = 0;
        foreach (var d in taskDone) if (d) done++;
        counterText.text = $"{done}/{taskTexts.Length}";
    }

    public bool AllTasksDone()
    {
        foreach (var d in taskDone) if (!d) return false;
        return true;
    }

    public (string label, bool done)[] GetTaskSnapshot()
    {
        var result = new (string, bool)[taskTexts.Length];
        for (int i = 0; i < taskTexts.Length; i++)
            result[i] = (taskTexts[i] != null ? taskTexts[i].text : "", taskDone[i]);
        return result;
    }

    IEnumerator BounceText(Transform t)
    {
        Vector3 orig = t.localScale;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(elapsed / 0.3f * Mathf.PI) * 0.25f;
            t.localScale = orig * s;
            yield return null;
        }
        t.localScale = orig;
    }

    // ── Pause ─────────────────────────────────────────────────────────────

    void TogglePause()
    {
        if (resultPanel != null && resultPanel.activeSelf) return;
        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void Resume()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RestartLevel() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void QuitToMap()    { Time.timeScale = 1f; SceneManager.LoadScene("OverworldMap"); }
    void LoadNextLevel(){ Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); }

    // ── Result Screens ────────────────────────────────────────────────────

    void ShowWinScreen()
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        string grade   = GameManager.Instance?.CalculateGrade() ?? "S";
        float timeLeft = GameManager.Instance?.TimeRemaining ?? 0f;

        resultHeader.text  = "WIN";  resultHeader.color = winColor;
        gradeText.text     = grade;  gradeText.color    = winColor;
        scoreText.text     = $"SCORE: {grade} — Tasks Complete!";
        finalTimeText.text = $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        if (failureReasonsText != null) failureReasonsText.gameObject.SetActive(false);

        nextLevelButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(false);
    }

    void ShowLossScreen()
    {
        if (resultPanel == null) return;
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        float timeLeft = GameManager.Instance?.TimeRemaining ?? 0f;
        var reasons    = GameManager.Instance?.LastFailureReasons;

        resultHeader.text  = "LOSE";  resultHeader.color = loseColor;
        gradeText.text     = "F";     gradeText.color    = loseColor;
        scoreText.text     = "SCORE: F";
        finalTimeText.text = timeLeft <= 0f ? "TIME EXPIRED: 00:00"
                           : $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        if (failureReasonsText != null)
        {
            if (reasons != null && reasons.Count > 0)
            {
                failureReasonsText.gameObject.SetActive(true);
                failureReasonsText.text = "• " + string.Join("\n• ", reasons);
            }
            else
            {
                failureReasonsText.gameObject.SetActive(false);
            }
        }

        nextLevelButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
    }
}
