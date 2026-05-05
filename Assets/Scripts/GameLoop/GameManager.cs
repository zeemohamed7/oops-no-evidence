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
            SuspicionMeter.Instance.OnGameOver.RemoveListener(TriggerLoss);
    }

    void TryHookSuspicion()
    {
        if (SuspicionMeter.Instance != null)
        {
            SuspicionMeter.Instance.OnGameOver.AddListener(TriggerLoss);
        }
    }
    
    
    private void Update()
    {
        if (!IsPlaying) return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TriggerLoss();
        }
    }

    // Call this when all checklist tasks are done AND players are in the van
    public void TriggerWin()
    {
        if (!IsPlaying) return;
        State = GameState.Won;
        OnWin.Invoke();
        Debug.Log($"WIN — Grade: {CalculateGrade()} | Time left: {FormatTime(TimeRemaining)}");
    }

    public void TriggerLoss()
    {
        if (!IsPlaying) return;
        State = GameState.Lost;
        OnLoss.Invoke();
        Debug.Log("LOSS");
    }

    // S-F grading: weighted average of time remaining and low suspicion
    // S = near perfect, F = barely scraped by or failed tasks
    public string CalculateGrade()
    {
        float timeScore = TimeRemaining / levelDuration; // 0–1

        float suspicionScore = 0f;
        if (SuspicionMeter.Instance != null)
            suspicionScore = 1f - (SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion);

        float total = (timeScore * 0.5f) + (suspicionScore * 0.5f); // equal weight

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
}
