using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Waiting, Playing, Won, Lost }

    [Header("Timer")]
    public float levelDuration = 360f;

    [Header("Events")]
    public UnityEvent OnWin;
    public UnityEvent OnLoss;

    public GameState State { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsPlaying => State == GameState.Playing;
    public int ActivePlayerCount { get; private set; }

    public List<string> LastFailureReasons { get; private set; } = new List<string>();

    [Header("Win/Lose Panel")]
    public WinLoseScreenManager winLosePanel;   // drag manager-win-lose here

    [Header("Level Setup")]
    public Transform truckSpawnPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        TimeRemaining = levelDuration;
        State = GameState.Waiting;  // timer only starts when truck arrives
    }

    private void OnEnable()
    {
        TryHookSuspicion();
    }

    private void OnDisable()
    {
        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.OnGameOver.RemoveListener(OnSuspicionGameOver);
    }

    void TryHookSuspicion()
    {
        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.OnGameOver.AddListener(OnSuspicionGameOver);
    }

    void OnSuspicionGameOver()
    {
        if (MissionResultManager.Instance != null)
            MissionResultManager.Instance.ForceSuspicionLoss();
        else
            TriggerLoss();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            if (MissionResultManager.Instance != null)
                MissionResultManager.Instance.ForceTimerLoss();
            else
                TriggerLoss();
        }
    }

    public void RefreshActivePlayerCount()
    {
        ActivePlayerCount = GameObject.FindGameObjectsWithTag("Player").Length;
        Debug.Log("Active players: " + ActivePlayerCount);
    }

    // ── Win / Loss ────────────────────────────────────────────────────────────

    public void TriggerWin()
    {
        if (!IsPlaying) return;
        State = GameState.Won;
        LastFailureReasons.Clear();
        UnlockNextLevel();
        OnWin.Invoke();
        Debug.Log($"WIN — Grade: {CalculateGrade()} | Time left: {FormatTime(TimeRemaining)}");
        LoadWinLoseScene(isWin: true, failures: null);
    }

    void UnlockNextLevel()
    {
        int levelIndex = LobbyManager.Instance != null ? LobbyManager.Instance.currentLevelIndex : -1;
        if (levelIndex < 0) return;

        int currentLevelNum = levelIndex + 1;          // 0-indexed → 1-indexed
        int highestReached  = PlayerPrefs.GetInt("ReachedLevel", 1);

        if (currentLevelNum >= highestReached)
        {
            PlayerPrefs.SetInt("ReachedLevel", Mathf.Min(currentLevelNum + 1, 4));
            PlayerPrefs.Save();
        }
    }

    public void TriggerLoss()
    {
        TriggerLoss(new List<string>());
    }

    public void TriggerLoss(List<string> reasons)
    {
        if (!IsPlaying) return;
        LastFailureReasons = reasons ?? new List<string>();

        string grade = CalculateGrade();
        bool isWin = grade != "D";   // S/A/B/C = win panel, D = lose panel

        if (isWin)
        {
            State = GameState.Won;
            UnlockNextLevel();
            OnWin.Invoke();
        }
        else
        {
            State = GameState.Lost;
            OnLoss.Invoke();
        }

        Debug.Log($"{(isWin ? "WIN" : "LOSS")} — Grade: {grade}");
        LoadWinLoseScene(isWin: isWin, failures: isWin ? null : LastFailureReasons);
    }

    void LoadWinLoseScene(bool isWin, List<string> failures)
    {
        if (winLosePanel != null)
        {
            if (isWin) winLosePanel.ShowWin(this, GameHUD.Instance);
            else       winLosePanel.ShowLoss(this, GameHUD.Instance);
            return;
        }

        // No in-scene panel wired — fall back to the separate win-lose scene.
        string grade = CalculateGrade();
        float  sus01 = SuspicionMeter.Instance != null
                       ? SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion
                       : 0f;
        var tasks = GameHUD.Instance?.GetTaskSnapshot();

        WinLoseScreenManager.SaveResultToPrefs(isWin, grade, TimeRemaining, sus01, failures, tasks);
        SceneManager.LoadScene("win-lose");
    }

    // ── Grading ───────────────────────────────────────────────────────────────

    public string CalculateGrade()
    {
        // Count completed tasks from the HUD snapshot
        int doneCount = 0, totalCount = 0;
        var snapshot = GameHUD.Instance?.GetTaskSnapshot();
        if (snapshot != null)
        {
            totalCount = snapshot.Length;
            foreach (var t in snapshot) if (t.done) doneCount++;
        }
        bool allDone = totalCount > 0 && doneCount >= totalCount;

        float sus01 = SuspicionMeter.Instance != null
            ? SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion
            : 0f;
        bool susHigh = sus01 >= 0.5f;

        // S: all done + sus low
        if (allDone && !susHigh) return "S";

        // A: (all done + sus high) OR (3+ done + sus low)
        if ((allDone && susHigh) || (doneCount >= 3 && !susHigh)) return "A";

        // B: (3+ done + sus high) OR (2+ done + sus low)
        if ((doneCount >= 3 && susHigh) || (doneCount >= 2 && !susHigh)) return "B";

        // C: (2+ done + sus high) OR (1+ done + sus low)
        if ((doneCount >= 2 && susHigh) || (doneCount >= 1 && !susHigh)) return "C";

        // D: 0 done OR (1 done + sus high)
        return "D";
    }

    public string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m:00}:{s:00}";
    }

    // ── Level Start ───────────────────────────────────────────────────────────

    public void OnTruckStopped()
    {
        Debug.Log("TRUCK STOPPED!");
        LobbyManager.Instance?.SpawnAllPlayers(truckSpawnPoint);
        RefreshActivePlayerCount();
        StartLevel();
    }

    // private void StartLevel()
    // {
    //     State = GameState.Playing;
    //     TimeRemaining = levelDuration;
    //
    //     if (SuspicionMeter.Instance != null)
    //         SuspicionMeter.Instance.globalSuspicion = 0f;
    //
    //     Debug.Log("The heist has begun! Start cleaning!");
    // }
    private void StartLevel()
    {
        State = GameState.Playing;
        TimeRemaining = levelDuration;

        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.globalSuspicion = 0f;

        // 🟢 FORCE ACTIVATION: Wake up the input system AND the movement controller scripts!
        TopDownPlayerController[] movementControllers = FindObjectsByType<TopDownPlayerController>(FindObjectsSortMode.None);
    
        foreach (TopDownPlayerController controller in movementControllers)
        {
            // 1. Force the script component to turn ON
            controller.enabled = true;

            // 2. Force the PlayerInput component on that same object to turn ON and map correctly
            PlayerInput pInput = controller.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                pInput.enabled = true;
                pInput.SwitchCurrentActionMap("Player");
                pInput.neverAutoSwitchControlSchemes = true;
            }

            Debug.Log($"[LEVEL START] Fully activated and initialized player controller script components for: {controller.gameObject.name}");
        }

        Debug.Log("The heist has begun! Start cleaning!");
    }
}