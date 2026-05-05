using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance;
    private void Awake() { Instance = this; }

    // ── TIMER ─────────────────────────────────────────────────────────────
    [Header("Timer")]
    public TextMeshProUGUI timerText;           // TimePanel > time
    public float timerWarningThreshold  = 60f;
    public float timerCriticalThreshold = 30f;
    public Color timerNormalColor   = Color.white;
    public Color timerWarningColor  = new Color(1f, 0.85f, 0f);
    public Color timerCriticalColor = new Color(0.9f, 0.1f, 0.1f);

    // ── SUSPICION ─────────────────────────────────────────────────────────
    [Header("Suspicion Bar")]
    public Image suspicionFill;             // susPanel > fill  (set Image Type → Filled, Horizontal)
    public TextMeshProUGUI suspicionText;   // susPanel > text
    float suspicionVisual;

    [Header("Suspicion State Colors")]
    public Color calmColor       = new Color(0.3f, 0.85f, 0.3f);
    public Color suspiciousColor = new Color(1f,   0.85f, 0f);
    public Color alertColor      = new Color(1f,   0.5f,  0f);
    public Color panicColor      = new Color(0.9f, 0.1f,  0.1f);

    [Header("Suspicion Messages")]
    public string[] lowSuspicionMsgs  = { "All quiet...", "Keep it clean." };
    public string[] midSuspicionMsgs  = { "They're looking!", "Watch out!" };
    public string[] highSuspicionMsgs = { "GET OUT!", "THEY KNOW!" };
    int currentSuspicionStage = -1;

    // ── TASKS ─────────────────────────────────────────────────────────────
    [System.Serializable]
    public class TaskEntry
    {
        public string taskName;     // must match what other scripts pass to CompleteTask()
        public GameObject row;      // drag task-1 / task-2 / task-3 here
        [HideInInspector] public bool isCompleted;
    }

    [Header("Tasks")]
    public List<TaskEntry> tasks = new List<TaskEntry>();
    public TextMeshProUGUI taskCountText;   // TaskPanel > count  (shows "0/3")
    public Color taskDoneColor = new Color(0.4f, 0.9f, 0.4f);

    // ── PAUSE ─────────────────────────────────────────────────────────────
    [Header("Pause")]
    public Button pauseButton;
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitToMapButton;
    bool isPaused;

    // ── RESULT SCREEN ─────────────────────────────────────────────────────
    [Header("Result Screen")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultHeaderText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI scoreLineText;
    public TextMeshProUGUI finalTimerText;
    public Button nextLevelButton;
    public Button retryButton;
    public Button quitResultButton;

    [Header("Colors")]
    public Color winColor  = new Color(0.2f, 0.8f, 0.3f);
    public Color loseColor = new Color(0.9f, 0.2f, 0.2f);

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        pauseMenuPanel.SetActive(false);
        resultPanel.SetActive(false);

        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(RestartLevel);
        quitToMapButton.onClick.AddListener(QuitToMap);
        quitResultButton.onClick.AddListener(QuitToMap);
        nextLevelButton.onClick.AddListener(LoadNextLevel);
        retryButton.onClick.AddListener(RestartLevel);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWin.AddListener(ShowWinScreen);
            GameManager.Instance.OnLoss.AddListener(ShowLossScreen);
        }

        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.OnStateChangedEvent.AddListener(OnSuspicionStateChanged);

        RefreshTaskCount();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (GameManager.Instance == null) return;

        UpdateTimer();
        UpdateSuspicionBar();
    }

    // ── Timer ─────────────────────────────────────────────────────────────

    void UpdateTimer()
    {
        if (timerText == null) return;
        float t = GameManager.Instance.TimeRemaining;
        timerText.text  = GameManager.Instance.FormatTime(t);
        timerText.color = t <= timerCriticalThreshold ? timerCriticalColor
                        : t <= timerWarningThreshold  ? timerWarningColor
                        : timerNormalColor;
    }

    // ── Suspicion ─────────────────────────────────────────────────────────

    void UpdateSuspicionBar()
    {
        if (suspicionFill == null || SuspicionMeter.Instance == null) return;

        float target = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
        suspicionVisual = Mathf.Lerp(suspicionVisual, target, Time.deltaTime * 6f);

        // fillAmount only works if Image Type is set to Filled > Horizontal in Inspector
        suspicionFill.fillAmount = suspicionVisual;

        UpdateSuspicionText(suspicionVisual);
    }

    void UpdateSuspicionText(float progress)
    {
        if (suspicionText == null) return;
        int stage = progress < 0.33f ? 0 : progress < 0.66f ? 1 : 2;
        if (stage == currentSuspicionStage) return;
        currentSuspicionStage = stage;
        string[] pool = stage == 0 ? lowSuspicionMsgs : stage == 1 ? midSuspicionMsgs : highSuspicionMsgs;
        suspicionText.text = pool[Random.Range(0, pool.Length)];
    }

    void OnSuspicionStateChanged(SuspicionMeter.SuspicionState state)
    {
        if (suspicionFill == null) return;
        suspicionFill.color = state switch
        {
            SuspicionMeter.SuspicionState.Calm       => calmColor,
            SuspicionMeter.SuspicionState.Suspicious => suspiciousColor,
            SuspicionMeter.SuspicionState.Alert      => alertColor,
            SuspicionMeter.SuspicionState.Panic      => panicColor,
            _ => calmColor
        };
    }

    // ── Tasks ─────────────────────────────────────────────────────────────

    // Call from any script: GameHUD.Instance.CompleteTask("DISPOSE OF BODY");
    public void CompleteTask(string taskName)
    {
        var entry = tasks.Find(t => t.taskName == taskName);
        if (entry == null || entry.isCompleted) return;

        entry.isCompleted = true;

        // Tint the row green and bounce it
        if (entry.row != null)
        {
            foreach (var tmp in entry.row.GetComponentsInChildren<TextMeshProUGUI>())
                tmp.color = taskDoneColor;
            StartCoroutine(BounceRow(entry.row.transform));
        }

        RefreshTaskCount();

        if (AllTasksDone())
            Debug.Log("All tasks done — waiting for players to reach the van.");
    }

    void RefreshTaskCount()
    {
        if (taskCountText == null) return;
        int done  = tasks.FindAll(t => t.isCompleted).Count;
        taskCountText.text = $"{done}/{tasks.Count}";
    }

    public bool AllTasksDone() => tasks.TrueForAll(t => t.isCompleted);

    IEnumerator BounceRow(Transform target)
    {
        Vector3 original = target.localScale;
        float elapsed = 0f, duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.25f;
            target.localScale = original * s;
            yield return null;
        }
        target.localScale = original;
    }

    // ── Pause ─────────────────────────────────────────────────────────────

    void TogglePause()
    {
        if (resultPanel.activeSelf) return;
        isPaused = !isPaused;
        pauseMenuPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    void Resume()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void QuitToMap()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("OverworldMap");
    }

    void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // ── Result Screens ────────────────────────────────────────────────────

    void ShowWinScreen()
    {
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        string grade  = GameManager.Instance?.CalculateGrade() ?? "S";
        float timeLeft = GameManager.Instance?.TimeRemaining ?? 0f;

        resultHeaderText.text  = "WIN";
        resultHeaderText.color = winColor;
        gradeText.text  = grade;
        gradeText.color = winColor;
        scoreLineText.text  = $"SCORE: {grade}\nTasks Complete!";
        finalTimerText.text = $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        nextLevelButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(false);
    }

    void ShowLossScreen()
    {
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        float timeLeft = GameManager.Instance?.TimeRemaining ?? 0f;

        resultHeaderText.text  = "LOSE";
        resultHeaderText.color = loseColor;
        gradeText.text  = "F";
        gradeText.color = loseColor;
        scoreLineText.text  = "SCORE: F";
        finalTimerText.text = timeLeft <= 0f
            ? "TIME EXPIRED: 00:00"
            : $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        nextLevelButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
    }
}
