using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One of up to four UI panels in the lobby scene.
/// Pure view layer — displays whatever a LobbyGhost tells it to.
/// No input logic lives here.
/// </summary>
public class LobbySlotUI : MonoBehaviour
{
    [Header("State Panels")]
    [SerializeField] private GameObject _emptyState;   // "Press button to join"
    [SerializeField] private GameObject _activeState;  // Character selection UI

    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI _playerLabel;
    [SerializeField] private TextMeshProUGUI _characterName;
    [SerializeField] private TextMeshProUGUI _readyLabel;
    [SerializeField] private TextMeshProUGUI _navHint;

    [Header("3D Preview")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private List<GameObject> _characterPrefabs = new();

    [Header("Preview Database (optional)")]
    [SerializeField] private CharacterPreviewDatabase _previewDb;

    private LobbyGhost _owner;
    private GameObject _currentCharacterInstance;

    public bool IsClaimed => _owner != null;

    // ─── LobbyGhost API ───────────────────────────────────────────────────────

    /// <summary>Register the ghost that owns this slot.</summary>
    public void Claim(LobbyGhost ghost)
    {
        _owner = ghost;
    }

    /// <summary>Activate the selection UI and label the slot with the player number.</summary>
    public void Initialize(int playerIndex)
    {
        _emptyState?.SetActive(false);
        _activeState?.SetActive(true);

        if (_playerLabel)  _playerLabel.text = $"Player {playerIndex + 1}";
        if (_navHint)      _navHint.text     = "Arrows to Change  |  R to Ready";
        if (_readyLabel)   _readyLabel.gameObject.SetActive(false);
    }

    /// <summary>Swap the displayed character and spawn the matching 3D preview.</summary>
    public void SetCharacter(string characterId, int index, int total)
    {
        if (_characterName) _characterName.text = characterId;

        // Tear down the old preview before building the new one
        if (_currentCharacterInstance != null)
            Destroy(_currentCharacterInstance);

        if (_spawnPoint != null && index >= 0 && index < _characterPrefabs.Count)
        {
            _currentCharacterInstance = Instantiate(
                _characterPrefabs[index],
                _spawnPoint.position,
                _spawnPoint.rotation,
                _spawnPoint
            );
        }
    }

    /// <summary>Show/hide the READY overlay and toggle the navigation hint.</summary>
    public void SetReady(bool isReady)
    {
        if (_readyLabel) _readyLabel.gameObject.SetActive(isReady);
        if (_navHint)    _navHint.gameObject.SetActive(!isReady);
    }

    /// <summary>Reset to the unoccupied waiting state.</summary>
    public void Release()
    {
        _owner = null;

        _emptyState?.SetActive(true);
        _activeState?.SetActive(false);

        if (_currentCharacterInstance != null)
        {
            Destroy(_currentCharacterInstance);
            _currentCharacterInstance = null;
        }
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _emptyState?.SetActive(true);
        _activeState?.SetActive(false);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ScriptableObject: maps character ID strings → preview prefabs.
// Create via: Assets > Create > Multiplayer > Character Preview Database
// ─────────────────────────────────────────────────────────────────────────────

[CreateAssetMenu(fileName = "CharacterPreviewDatabase",
                 menuName  = "Multiplayer/Character Preview Database")]
public class CharacterPreviewDatabase : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string     characterId;
        public GameObject previewPrefab;
    }

    [SerializeField] private Entry[] _entries;

    public GameObject GetPrefab(string characterId)
    {
        foreach (Entry e in _entries)
            if (e.characterId == characterId) return e.previewPrefab;

        Debug.LogWarning($"[CharacterPreviewDatabase] No prefab registered for '{characterId}'");
        return null;
    }
}