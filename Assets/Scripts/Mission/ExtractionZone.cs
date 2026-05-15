using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Place on the van/exit trigger collider (set to Is Trigger in Inspector).
/// Tracks which players have entered and fires OnAllPlayersExtracted once all are in.
///
/// Setup:
///   1. Add a trigger Collider to the van GameObject.
///   2. Attach this script.
///   3. Wire OnAllPlayersExtracted → MissionResultManager.EvaluateMission in the Inspector.
///   4. Make sure all player prefabs are tagged "Player".
///   5. Optionally set expectedPlayerCount to the known player count for this level.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExtractionZone : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Fired once all alive players are inside the zone.")]
    public UnityEvent OnAllPlayersExtracted;

    [Header("Player Detection")]
    [Tooltip("Tag assigned to player GameObjects. Must match the tag on the player prefab.")]
    public string playerTag = "Player";

    [Tooltip("How many players are expected to extract. Set to 0 to auto-detect from tagged objects.")]
    public int expectedPlayerCount ;

    [Header("Debug")]
    public bool logEvents = true;

    private readonly HashSet<GameObject> _playersInZone = new HashSet<GameObject>();
    private bool _extractionComplete = false;

    void Awake()
    {
        // Enforce trigger so players pass through instead of colliding
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        expectedPlayerCount = GameManager.Instance != null 
            ? GameManager.Instance.ActivePlayerCount 
            : 0;

        Debug.Log("Expected players for extraction: " + expectedPlayerCount);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("SOMETHING ENTERED: " + other.name);

        if (_extractionComplete) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        GameObject root = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!root.CompareTag(playerTag)) return;
        if (_playersInZone.Contains(root)) return;

        _playersInZone.Add(root);

        if (logEvents)
            Debug.Log($"[ExtractionZone] {root.name} entered van. ({_playersInZone.Count}/{GetExpectedCount()})");

        CheckExtractionComplete();
    }

    void CheckExtractionComplete()
    {
        int expected = GetExpectedCount();
        if (expected <= 0 || _playersInZone.Count < expected) return;

        _extractionComplete = true;

        if (logEvents) Debug.Log("[ExtractionZone] All players in van — triggering mission evaluation.");
        OnAllPlayersExtracted.Invoke();
    }

    int GetExpectedCount()
    {
        return expectedPlayerCount > 0
            ? expectedPlayerCount
            : GameObject.FindGameObjectsWithTag(playerTag).Length;
    }

    public int PlayersInZone => _playersInZone.Count;
    public bool IsComplete => _extractionComplete;
}
