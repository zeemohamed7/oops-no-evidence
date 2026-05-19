using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One of the 1–4 UI panels in the lobby scene.
/// Responsible only for DISPLAYING state — no input logic lives here.
/// A LobbyGhost calls the public methods to drive the visuals.
///
/// SCENE SETUP:
///   • Create a canvas panel per slot (Slot_1, Slot_2, Slot_3, Slot_4).
///   • Each needs this component + the referenced UI children below.
///   • The CharacterSpawnPoint is a world-space Transform where you instantiate
///     a 3D character preview mesh (or UI RawTexture from a RenderTexture).
/// </summary>
public class LobbySlotUI : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private GameObject  _emptyState;       // "Press Space/A to Join"
    [SerializeField] private GameObject  _activeState;      // The whole selection UI panel
    [SerializeField] private TextMeshProUGUI _playerLabel;  // "Player 1"
    [SerializeField] private TextMeshProUGUI _characterName;// "Zainab"
    [SerializeField] private TextMeshProUGUI _readyLabel;   // "READY!" overlay
    [SerializeField] private TextMeshProUGUI _navHint;      // "← → to change | R to ready"
    
    // Character Selection
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private List<GameObject> _characterPrefabs;
    private GameObject _currentCharacterInstance; // runtime state
    

    [Header("3D Preview")]
    [SerializeField] private Transform   _characterSpawnPoint;
    [SerializeField] private CharacterPreviewDatabase _previewDb; // SO with prefab lookup
    

    // ── State ──────────────────────────────────────────────────────────────
    private LobbyGhost       _owner;
    private GameObject       _currentPreviewInstance;

    public bool IsClaimed => _owner != null;

    // ── LobbyGhost API ─────────────────────────────────────────────────────

    /// <summary>Called by LobbyGhost immediately after claiming the slot.</summary>
    public void Claim(LobbyGhost ghost)
    {
        _owner = ghost;
    }

    /// <summary>Sets player number label and activates the selection UI.</summary>
    public void Initialize(int playerIndex)
    {
        _emptyState?.SetActive(false);
        _activeState?.SetActive(true);

        if (_navHint)      _navHint.text       = "Arrows to Change  |  R Ready";
        if (_readyLabel)   _readyLabel.gameObject.SetActive(false);
    }

    /// <summary>Updates the character name and spawns the correct 3D preview.</summary>
    public void SetCharacter(string characterId, int index, int total)
    {
        // Destroy old model
        if (_currentCharacterInstance != null)
        {
            Destroy(_currentCharacterInstance);
        }

        // Spawn new model
        if (index >= 0 && index < _characterPrefabs.Count)
        {
            _currentCharacterInstance = Instantiate(
                _characterPrefabs[index],
                _spawnPoint.position,
                _spawnPoint.rotation
            );
        }
    }
    /// <summary>Toggles the ready overlay and locks/unlocks the selector arrows.</summary>
    public void SetReady(bool isReady)
    {
        if (_readyLabel) _readyLabel.gameObject.SetActive(isReady);
        if (_navHint)    _navHint.gameObject.SetActive(!isReady);
    }

    /// <summary>Resets the slot back to its empty/waiting state.</summary>
    public void Release()
    {
        _owner = null;
        _emptyState?.SetActive(true);
        _activeState?.SetActive(false);

        if (_currentPreviewInstance != null)
        {
            Destroy(_currentPreviewInstance);
            _currentPreviewInstance = null;
        }
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start in empty state
        _emptyState?.SetActive(true);
        _activeState?.SetActive(false);
    }

    private void SpawnPreview(string characterId)
    {
        if (_characterSpawnPoint == null) return;

        // Destroy previous preview
        if (_currentPreviewInstance != null)
            Destroy(_currentPreviewInstance);

        if (_previewDb == null) return;

        GameObject prefab = _previewDb.GetPrefab(characterId);
        if (prefab != null)
        {
            _currentPreviewInstance = Instantiate(prefab, _characterSpawnPoint.position,
                                                   _characterSpawnPoint.rotation,
                                                   _characterSpawnPoint);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ScriptableObject: maps character IDs → preview prefabs
// Create via: Assets > Create > Multiplayer > Character Preview Database
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "CharacterPreviewDatabase",
                 menuName  = "Multiplayer/Character Preview Database")]
public class CharacterPreviewDatabase : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string     characterId;   // Must match the strings in LobbyGhost.Characters
        public GameObject previewPrefab; // A simple mesh with idle animation
    }

    [SerializeField] private Entry[] _entries;

    public GameObject GetPrefab(string characterId)
    {
        foreach (var e in _entries)
            if (e.characterId == characterId) return e.previewPrefab;

        Debug.LogWarning($"[CharacterPreviewDatabase] No prefab for '{characterId}'");
        return null;
    }
}