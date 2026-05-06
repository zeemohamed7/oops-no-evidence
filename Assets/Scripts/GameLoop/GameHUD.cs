using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameHUD : MonoBehaviour
{
    [System.Serializable]
    public class ChecklistTask
    {
        public string taskName;         // e.g. "DISPOSE OF BODY"
        public string progressLabel;    // e.g. "(1/1)" or "(Ongoing)"
        public bool isCompleted;
        [HideInInspector] public GameObject uiRow; // assigned at runtime
    }

    // ── HUD ──────────────────────────────────────────────────────────────
    [Header("HUD - Timer")]
    public TextMeshProUGUI timerText;

    [Header("HUD - Suspicion")]
    public Slider suspicionSlider;
    public RectTransform suspicionIcon; // The moving icon
    public TextMeshProUGUI suspicionStatusText; // The "Big Boss is watching" text
    float suspicionVisual;

    [Header("Suspicion Messages")]
    public string[] lowSuspicionMsgs = { "All quiet...", "Keep it clean." };
    public string[] midSuspicionMsgs = { "They're looking!", "Watch out!" };
    public string[] highSuspicionMsgs = { "GET OUT!", "THEY KNOW!" };
    private int currentSuspicionStage = -1;
    
    [Header("HUD - Checklist")]
    public Transform checklistContainer;   // Vertical Layout Group parent
    public GameObject checklistRowPrefab;  // Prefab: checkbox Image + taskName TMP + progress TMP
    public List<ChecklistTask> tasks = new List<ChecklistTask>();

    [Header("HUD - Pause Button")]
    public Button pauseButton;

    // ── PAUSE MENU ────────────────────────────────────────────────────────
    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitToMapButton;

    // ── RESULT SCREEN ─────────────────────────────────────────────────────
    [Header("Result Screen")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultHeaderText;    // "WIN" or "LOSE"
    public TextMeshProUGUI gradeText;           // "S" / "F"
    public TextMeshProUGUI scoreLineText;       // "SCORE: S" / "Tasks Complete!"
    public TextMeshProUGUI finalTimerText;      // "TIME REMAINING: 02:15" or "TIME EXPIRED: 00:00"
    public Slider finalSuspicionSlider;
    public Button nextLevelButton;
    public Button retryButton;
    public Button quitResultButton;

    // ── Colors ────────────────────────────────────────────────────────────
    [Header("Colors")]
    public Color winColor = new Color(0.2f, 0.8f, 0.3f);
    public Color loseColor = new Color(0.9f, 0.2f, 0.2f);

    private bool isPaused = false;

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        BuildChecklist();

        pauseMenuPanel.SetActive(false);
        resultPanel.SetActive(false);

        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(RestartLevel);
        quitToMapButton.onClick.AddListener(QuitToMap);
        quitResultButton.onClick.AddListener(QuitToMap);
        nextLevelButton.onClick.AddListener(LoadNextLevel);
        retryButton.onClick.AddListener(RestartLevel);

        // Listen for win/loss from GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWin.AddListener(ShowWinScreen);
            GameManager.Instance.OnLoss.AddListener(ShowLossScreen);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (GameManager.Instance == null) return;

        // 1. Update Timer
        if (timerText != null)
            timerText.text = GameManager.Instance.FormatTime(GameManager.Instance.TimeRemaining);

        // 2. Update Stylized Suspicion
        if (suspicionSlider != null && SuspicionMeter.Instance != null)
        {
            float target = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
        
            // Smooth transition (Lerp) for the bar
            suspicionVisual = Mathf.Lerp(suspicionVisual, target, Time.deltaTime * 6f);
            suspicionSlider.value = suspicionVisual;

            // Move the Mop/Icon (Matches LoadingCleanBar logic)
            if (suspicionIcon != null) {
                float barWidth = suspicionSlider.GetComponent<RectTransform>().rect.width;
                float startX = -barWidth / 2;
                float endX = barWidth / 2;
                float xPos = Mathf.Lerp(startX, endX, suspicionVisual);
                suspicionIcon.anchoredPosition = new Vector2(xPos, suspicionIcon.anchoredPosition.y);
            }

            // Update Warning Text Stages
            UpdateSuspicionText(suspicionVisual);
        }
    }

    void UpdateSuspicionText(float progress) {
        if (suspicionStatusText == null) return;

        int stage = (progress < 0.33f) ? 0 : (progress < 0.66f) ? 1 : 2;

        if (stage != currentSuspicionStage) {
            currentSuspicionStage = stage;
            string[] currentArray = stage == 0 ? lowSuspicionMsgs : stage == 1 ? midSuspicionMsgs : highSuspicionMsgs;
            suspicionStatusText.text = currentArray[Random.Range(0, currentArray.Length)];
        }
    }

    // ── Checklist ─────────────────────────────────────────────────────────

    void BuildChecklist()
    {
        foreach (var task in tasks)
        {
            GameObject row = Instantiate(checklistRowPrefab, checklistContainer);
            task.uiRow = row;
            RefreshTaskRow(task);
        }
    }

    void RefreshTaskRow(ChecklistTask task)
    {
        if (task.uiRow == null) return;

        // Expects prefab children: [0] = checkbox Image, [1] = taskName TMP, [2] = progress TMP
        var images = task.uiRow.GetComponentsInChildren<Image>();
        var texts  = task.uiRow.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 2)
        {
            texts[0].text = task.taskName;
            texts[1].text = task.progressLabel;
        }

        // Tick / untick checkbox (first Image is the checkbox)
        if (images.Length >= 1)
            images[0].color = task.isCompleted ? winColor : Color.white;
    }

    // Call this from other scripts when a task is done:
    // GameHUD.Instance.CompleteTask("DISPOSE OF BODY");
    public static GameHUD Instance;
    private void Awake() { Instance = this; }

    public void CompleteTask(string taskName)
    {
        var task = tasks.Find(t => t.taskName == taskName);
        if (task == null) return;
        task.isCompleted = true;
        RefreshTaskRow(task);

        if (AllTasksDone())
            Debug.Log("All tasks done — waiting for players to reach the van.");
    }

    public void UpdateTaskProgress(string taskName, string newProgress)
    {
        var task = tasks.Find(t => t.taskName == taskName);
        if (task == null) return;
        task.progressLabel = newProgress;
        RefreshTaskRow(task);
    }

    public bool AllTasksDone()
    {
        return tasks.TrueForAll(t => t.isCompleted);
    }

    // ── Pause ─────────────────────────────────────────────────────────────

    void TogglePause()
    {
        if (resultPanel.activeSelf) return; // don't pause on result screen
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
        // Replace "OverworldMap" with your actual map scene name when ready
        SceneManager.LoadScene("OverworldMap");
    }

    void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(next);
    }

    // ── Result Screens ────────────────────────────────────────────────────

    void ShowWinScreen()
    {
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        string grade = GameManager.Instance != null ? GameManager.Instance.CalculateGrade() : "S";
        float timeLeft = GameManager.Instance != null ? GameManager.Instance.TimeRemaining : 0f;

        resultHeaderText.text = "WIN";
        resultHeaderText.color = winColor;
        gradeText.text = grade;
        gradeText.color = winColor;
        scoreLineText.text = $"SCORE: {grade}\nTasks Complete!";
        finalTimerText.text = $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        if (finalSuspicionSlider != null && SuspicionMeter.Instance != null)
            finalSuspicionSlider.value = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;

        nextLevelButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(false);
    }

    void ShowLossScreen()
    {
        resultPanel.SetActive(true);
        Time.timeScale = 0f;

        string grade = "F";
        float timeLeft = GameManager.Instance != null ? GameManager.Instance.TimeRemaining : 0f;

        resultHeaderText.text = "LOSE";
        resultHeaderText.color = loseColor;
        gradeText.text = grade;
        gradeText.color = loseColor;
        scoreLineText.text = $"SCORE: {grade}";
        finalTimerText.text = timeLeft <= 0f
            ? "TIME EXPIRED: 00:00"
            : $"TIME REMAINING: {GameManager.Instance.FormatTime(timeLeft)}";

        if (finalSuspicionSlider != null && SuspicionMeter.Instance != null)
            finalSuspicionSlider.value = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;

        nextLevelButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
    }
}
