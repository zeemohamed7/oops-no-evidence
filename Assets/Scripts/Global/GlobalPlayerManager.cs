using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stores confirmed player selections across scene loads.
/// Lives as a singleton MonoBehaviour on a DontDestroyOnLoad object,
/// OR can be driven by a ScriptableObject (see GlobalPlayerData SO below).
/// 
/// Recommended: Use the Singleton pattern here and keep GlobalPlayerData
/// as the pure data container you serialize/pass around.
/// </summary>

// ─────────────────────────────────────────────────────────────────────────────
// Data container — pure serializable struct, no Unity dependencies
// ─────────────────────────────────────────────────────────────────────────────
[System.Serializable]
public struct PlayerSelectionData
{
    public int      playerIndex;    // 0–3
    public string   characterId;    // e.g. "Zainab", "Hajar", etc.
    public string   controlScheme;  // e.g. "KeyboardWASD", "Gamepad1"
    public int      deviceId;       // InputDevice.deviceId for re-pairing in game scene

    public override string ToString() =>
        $"[P{playerIndex}] {characterId} | {controlScheme} (Device {deviceId})";
}

// ─────────────────────────────────────────────────────────────────────────────
// ScriptableObject — optional if you want inspector visibility & asset refs
// Create via: Assets > Create > Multiplayer > Global Player Data
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "GlobalPlayerData", menuName = "Multiplayer/Global Player Data")]
public class GlobalPlayerData : ScriptableObject
{
    [Header("Confirmed Selections (populated at lobby ready)")]
    public List<PlayerSelectionData> confirmedPlayers = new();

    public void Clear() => confirmedPlayers.Clear();

    public void AddOrUpdate(PlayerSelectionData data)
    {
        int idx = confirmedPlayers.FindIndex(p => p.playerIndex == data.playerIndex);
        if (idx >= 0) confirmedPlayers[idx] = data;
        else          confirmedPlayers.Add(data);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Singleton manager — persists across scenes, wraps the SO
// Attach to a GameObject in your first scene (or the Lobby scene)
// ─────────────────────────────────────────────────────────────────────────────
public class GlobalPlayerManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Data Asset (assign in inspector)")]
    [SerializeField] private GlobalPlayerData _data;

    // ── Singleton ──────────────────────────────────────────────────────────
    public static GlobalPlayerManager Instance { get; private set; }

    // ── Public accessors ───────────────────────────────────────────────────
    public static IReadOnlyList<PlayerSelectionData> Players =>
        Instance._data.confirmedPlayers;

    // ── Unity lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _data.Clear(); // Always start fresh on boot
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Call this from LobbyManager when all players hit Ready.</summary>
    public static void SavePlayerSelection(PlayerSelectionData data)
    {
        if (Instance == null)
        {
            Debug.LogError("[GlobalPlayerManager] No instance in scene!");
            return;
        }
        Instance._data.AddOrUpdate(data);
        Debug.Log($"[GlobalPlayerManager] Saved: {data}");
    }

    /// <summary>
    /// Call at the start of your combat scene to re-pair devices to PlayerInputs.
    /// Returns the InputDevice that was used in the lobby, or null if not found.
    /// </summary>
    public static InputDevice GetDeviceForPlayer(int playerIndex)
    {
        if (Instance == null) return null;

        var entry = Instance._data.confirmedPlayers.Find(p => p.playerIndex == playerIndex);
        return InputSystem.GetDeviceById(entry.deviceId);
    }

    public static void ClearAll() => Instance?._data.Clear();
}