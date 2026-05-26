using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent OnAllPlayersExtracted;

    [Header("Player Detection")]
    public string playerTag = "Player";

    [Tooltip("Override expected player count. Leave 0 to auto-detect from GameManager.")]
    public int expectedPlayerCount;

    [Header("Task Gate")]
    [Tooltip("Optional solid (non-trigger) collider at truck entrance — disabled when all tasks done.")]
    public GameObject entranceBlocker;

    [Header("UI Feedback")]
    [Tooltip("The Truckmsg TMP text object — shown only when a player is near the truck.")]
    public TextMeshProUGUI promptText;

    [Tooltip("How close a player must be to the truck to see the message.")]
    public float promptRadius = 6f;

    [Header("Debug")]
    public bool logEvents = true;

    // State
    private readonly HashSet<GameObject> _playersInZone = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, List<Renderer>> _hiddenRenderers = new Dictionary<GameObject, List<Renderer>>();
    private bool _extractionComplete = false;
    private bool _tasksWereDone     = false;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (expectedPlayerCount <= 0 && GameManager.Instance != null)
            expectedPlayerCount = GameManager.Instance.ActivePlayerCount;

        if (entranceBlocker != null) entranceBlocker.SetActive(true);

        GameEvents.OnTaskCompleted += OnAnyTaskCompleted;
    }

    void Update()
    {
        if (_extractionComplete) return;

        bool playerNearby = IsAnyPlayerNearby();
        if (promptText != null)
            promptText.gameObject.SetActive(playerNearby);

        if (playerNearby)
            RefreshPrompt();
    }

    bool IsAnyPlayerNearby()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag(playerTag))
        {
            if (Vector3.Distance(transform.position, p.transform.position) <= promptRadius)
                return true;
        }
        return false;
    }

    void OnDestroy()
    {
        GameEvents.OnTaskCompleted -= OnAnyTaskCompleted;
    }

    // ── Task gate ──────────────────────────────────────────────────────────

    void OnAnyTaskCompleted(string _)
    {
        if (_tasksWereDone) return;
        if (GameHUD.Instance == null || !GameHUD.Instance.AllTasksDone()) return;

        _tasksWereDone = true;
        if (entranceBlocker != null) entranceBlocker.SetActive(false);
        if (logEvents) Debug.Log("[ExtractionZone] All tasks done — truck unlocked.");
        RefreshPrompt();
    }

    bool TasksComplete()
    {
        if (_tasksWereDone) return true;
        return GameHUD.Instance == null || GameHUD.Instance.AllTasksDone();
    }

    // ── Trigger detection ──────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (_extractionComplete) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!root.CompareTag(playerTag)) return;

        if (!TasksComplete())
        {
            // Tasks not done — bump them out visually via message, blocker handles physics
            RefreshPrompt(); // ensures "tasks not done" text is shown
            if (logEvents) Debug.Log($"[ExtractionZone] {root.name} blocked — tasks not done.");
            return;
        }

        if (_playersInZone.Contains(root)) return;
        _playersInZone.Add(root);
        HidePlayer(root);
        RefreshPrompt();

        if (logEvents)
            Debug.Log($"[ExtractionZone] {root.name} boarded truck. ({_playersInZone.Count}/{GetExpectedCount()})");

        CheckExtractionComplete();
    }

    void OnTriggerExit(Collider other)
    {
        if (_extractionComplete) return;

        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!root.CompareTag(playerTag)) return;
        if (!_playersInZone.Remove(root)) return;

        ShowPlayer(root);
        RefreshPrompt();

        if (logEvents)
            Debug.Log($"[ExtractionZone] {root.name} left truck. ({_playersInZone.Count}/{GetExpectedCount()})");
    }

    // ── Win check ──────────────────────────────────────────────────────────

    void CheckExtractionComplete()
    {
        int expected = GetExpectedCount();
        if (expected <= 0 || _playersInZone.Count < expected) return;

        _extractionComplete = true;
        if (promptText != null) promptText.gameObject.SetActive(false);
        if (logEvents) Debug.Log("[ExtractionZone] All players at truck — triggering win!");

        OnAllPlayersExtracted.Invoke();

        if (OnAllPlayersExtracted.GetPersistentEventCount() == 0)
        {
            if (MissionResultManager.Instance != null)
                MissionResultManager.Instance.EvaluateMission();
            else
                GameManager.Instance?.TriggerWin();
        }
    }

    // ── Player hide / show ─────────────────────────────────────────────────

    void HidePlayer(GameObject player)
    {
        var renderers = new List<Renderer>(player.GetComponentsInChildren<Renderer>());
        _hiddenRenderers[player] = renderers;
        foreach (var r in renderers) r.enabled = false;
    }

    void ShowPlayer(GameObject player)
    {
        if (!_hiddenRenderers.TryGetValue(player, out var renderers)) return;
        foreach (var r in renderers) if (r != null) r.enabled = true;
        _hiddenRenderers.Remove(player);
    }

    // ── UI prompt ──────────────────────────────────────────────────────────

    void RefreshPrompt()
    {
        if (promptText == null) return;

        if (!TasksComplete())
        {
            promptText.text = "Complete all tasks first!";
            return;
        }

        int expected = GetExpectedCount();
        int inZone   = _playersInZone.Count;

        if (inZone == 0)
            promptText.text = "ALL DONE! Go to the Truck!";
        else if (inZone >= expected)
            promptText.text = "All players ready!";
        else
            promptText.text = $"{inZone}/{expected} players at truck";
    }

    int GetExpectedCount()
    {
        if (expectedPlayerCount > 0) return expectedPlayerCount;
        if (GameManager.Instance != null && GameManager.Instance.ActivePlayerCount > 0)
            return GameManager.Instance.ActivePlayerCount;
        return GameObject.FindGameObjectsWithTag(playerTag).Length;
    }

    public int PlayersInZone => _playersInZone.Count;
    public bool IsComplete   => _extractionComplete;
}