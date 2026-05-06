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
    
    public void OnTruckStopped()
    {
        // 1. Tell the LobbyManager to teleport the players here
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.SpawnAllPlayers(truckSpawnPoint);
        }

        // 2. Set the state to 'Playing' so the timer starts
        StartLevel();
    }
    
    // Incomplete - state change happens, HUD reveal, passive suspicion (starts listening to guard's vision cone)
    private void StartLevel()
    {
        // 1. Change the state so the Update() loop begins counting down
        State = GameState.Playing; 
    
        // 2. Set the timer to your level duration (e.g., 360 seconds)
        TimeRemaining = levelDuration; 

        // 3. Reset the Suspicion Meter to 0 so the player starts with a clean slate
        if (SuspicionMeter.Instance != null)
        {
            SuspicionMeter.Instance.globalSuspicion = 0f;
        }

        // 4. Reveal the HUD / UI
        // If your GameHUD has an 'In-Game' panel, you could activate it here.
        if (GameHUD.Instance != null)
        {
            // This ensures the checklist and bar are visible to the player
            Debug.Log("HUD: Checklist and Suspicion Bar revealed.");
        }

        Debug.Log("The heist has begun! Start cleaning!");
    }
}
