using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState { Playing, Won, Lost }

    [Header("Timer")]
    public float levelDuration = 360f; // 6 minutes default

    [Header("Events")]
    public UnityEvent OnWin;
    public UnityEvent OnLoss;

    public GameState State { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsPlaying => State == GameState.Playing;
    public int ActivePlayerCount { get; private set; }

    /// <summary>Populated by TriggerLoss(reasons) — read by GameHUD to show failure details.</summary>
    public List<string> LastFailureReasons { get; private set; } = new List<string>();

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
        State = GameState.Playing;
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

    // Routes suspicion-max loss through MissionResultManager when available
    // so the failure reason gets captured. Falls back to direct TriggerLoss.
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
            // Routes through MissionResultManager so the failure reason is captured
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
        OnWin.Invoke();
        Debug.Log($"WIN — Grade: {CalculateGrade()} | Time left: {FormatTime(TimeRemaining)}");
    }

    public void TriggerLoss()
    {
        TriggerLoss(new List<string>());
    }

    public void TriggerLoss(List<string> reasons)
    {
        if (!IsPlaying) return;
        State = GameState.Lost;
        LastFailureReasons = reasons ?? new List<string>();
        OnLoss.Invoke();
        Debug.Log($"LOSS — {string.Join(" | ", LastFailureReasons)}");
    }

    // ── Grading ───────────────────────────────────────────────────────────────

    // S-F grading: weighted average of time remaining and low suspicion
    public string CalculateGrade()
    {
        float timeScore = TimeRemaining / levelDuration;

        float suspicionScore = 0f;
        if (SuspicionMeter.Instance != null)
            suspicionScore = 1f - (SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion);

        float total = (timeScore * 0.5f) + (suspicionScore * 0.5f);

        if (total >= 0.90f) return "S";
        if (total >= 0.75f) return "A";
        if (total >= 0.60f) return "B";
        if (total >= 0.45f) return "C";
        if (total >= 0.30f) return "D";
        return "F";
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

    private void StartLevel()
    {
        State = GameState.Playing;
        TimeRemaining = levelDuration;

        if (SuspicionMeter.Instance != null)
            SuspicionMeter.Instance.globalSuspicion = 0f;

        if (GameHUD.Instance != null)
            Debug.Log("HUD: Checklist and Suspicion Bar revealed.");

        Debug.Log("The heist has begun! Start cleaning!");
    }
}
