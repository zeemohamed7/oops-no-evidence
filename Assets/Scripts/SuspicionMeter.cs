using UnityEngine;
using UnityEngine.Events;

public class SuspicionMeter : MonoBehaviour
{
    public static SuspicionMeter Instance;
    
    public enum SuspicionState
    {
        Calm,
        Suspicious,
        Alert,
        Panic
    }
    
    public SuspicionState CurrentState { get; private set; }

    [Header("Global Meter")]
    public float globalSuspicion;
    public float maxSuspicion = 100f;

    [Header("Tuning")]
    public float passiveDecayRate = 2f;
    public float decayDelay = 2f; // time after last increase before decay starts

    [Header("Events")]
    public UnityEvent OnGameOver;

    private bool hasTriggeredGameOver = false;
    private float lastIncreaseTime;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ResetMeter();
    }

    private void OnEnable()
    {
        GameEvents.OnSuspicionAdded += AddSuspicion;
    }

    private void OnDisable()
    {
        GameEvents.OnSuspicionAdded -= AddSuspicion;
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            return;

        HandleDecay();
        UpdateState();
    }
    
    void UpdateState()
    {
        float percent = globalSuspicion / maxSuspicion;

        SuspicionState newState;

        if (percent < 0.25f)
            newState = SuspicionState.Calm;
        else if (percent < 0.5f)
            newState = SuspicionState.Suspicious;
        else if (percent < 0.75f)
            newState = SuspicionState.Alert;
        else
            newState = SuspicionState.Panic;

        if (newState != CurrentState)
        {
            CurrentState = newState;
            OnStateChanged();
        }
    }
    
    void OnStateChanged()
    {
        Debug.Log($"Suspicion State: {CurrentState}");

        switch (CurrentState)
        {
            case SuspicionState.Calm:
                // soft UI, relaxed guards
                break;

            case SuspicionState.Suspicious:
                // subtle audio tension, guards investigate more
                break;

            case SuspicionState.Alert:
                // music ramps, guards more aggressive
                break;

            case SuspicionState.Panic:
                // heavy tension, near-fail state
                break;
        }
    }

    // ─────────────────────────────────────────────
    // Core Logic
    // ─────────────────────────────────────────────

    void AddSuspicion(float amount)
    {
        if (hasTriggeredGameOver) return;

        lastIncreaseTime = Time.time;
        ModifySuspicion(amount);
    }

    void HandleDecay()
    {
        if (globalSuspicion <= 0f) return;

        // Only decay after delay since last alert
        if (Time.time - lastIncreaseTime < decayDelay)
            return;

        ModifySuspicion(-passiveDecayRate * Time.deltaTime);
    }

    void ModifySuspicion(float amount)
    {
        if (hasTriggeredGameOver) return;

        globalSuspicion += amount;
        globalSuspicion = Mathf.Clamp(globalSuspicion, 0f, maxSuspicion);

        if (globalSuspicion >= maxSuspicion)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        if (hasTriggeredGameOver) return;

        hasTriggeredGameOver = true;
        OnGameOver?.Invoke();

        Debug.Log("GAME OVER - Suspicion Maxed");
    }

    // ─────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────

    public void ResetMeter()
    {
        globalSuspicion = 0f;
        hasTriggeredGameOver = false;
        lastIncreaseTime = -999f;
    }
}