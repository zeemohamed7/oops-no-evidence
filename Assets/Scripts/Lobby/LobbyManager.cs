using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/// <summary>
/// Manages the lobby lifecycle:
///   1. Listens for join inputs via a dedicated "join listener" PlayerInput.
///   2. Spawns Ghost prefabs via PlayerInputManager using the Manual join scheme.
///   3. Tracks slot ownership and signals when all players are ready.
///
/// SCENE SETUP REQUIREMENTS:
///   • This GameObject needs a PlayerInputManager component.
///   • PlayerInputManager settings:
///       - Join Behavior: "Join Players Manually"  ← critical
///       - Player Prefab: your LobbyGhost prefab
///       - Notification: "Invoke Unity Events" or "Send Messages" (either works)
///   • Place 1–4 LobbySlotUI objects in the scene, tagged or referenced below.
///   • Reference LobbyInputActions asset in the inspector.
/// </summary>
[RequireComponent(typeof(PlayerInputManager))]
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;
    [Header("UI Panels")]
    [SerializeField] private GameObject _mainMenuPanel;     
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private GameObject _settingsPanel;
    
    public void OpenCredits() => SwitchPanel(_creditsPanel);
    public void OpenSettings() => SwitchPanel(_settingsPanel);
    public void BackToMain() => SwitchPanel(_mainMenuPanel);

    private void SwitchPanel(GameObject target)
    {
        _mainMenuPanel.SetActive(false);
        if (_creditsPanel) _creditsPanel.SetActive(false);
        if (_settingsPanel) _settingsPanel.SetActive(false);

        target.SetActive(true);
    }
    
    [Header("Level Selection")]
    public string selectedLevelName = "Level1_ShawarmaShop"; // Default level
    
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Input")]
    [SerializeField] private InputActionAsset _lobbyInputAsset;

    [Header("Slots (assign Slot_1 → Slot_4 in order)")]
    [SerializeField] private List<LobbySlotUI> _slots = new();

    [Header("Scene")]
    [SerializeField] private string _combatSceneName = "CombatScene";

    [Header("Debug")]
    [SerializeField] private bool _allowKeyboardWASD = true;

    // ── Private state ──────────────────────────────────────────────────────
    private PlayerInputManager       _pim;
    private InputAction              _joinAction;
    [SerializeField] private GameObject _ghostPrefab;

    // Maps deviceId → ghost, so we never double-join the same device
    private readonly Dictionary<int, LobbyGhost> _activeGhosts = new();
    public List<PlayerSelectionData> playersToSpawn = new List<PlayerSelectionData>();

    // Control scheme names — must match exactly what's in your .inputactions asset
    private const string SchemeKeyboard = "KeyboardWASD";
    private const string SchemeGamepad  = "Gamepad";   // prefix; actual names: Gamepad1, Gamepad2…

    [SerializeField] private Slider _volumeSlider;

    void Start()
    {
        // 1. Set the actual game volume to 100%
        AudioListener.volume = 1f;

        // 2. Make sure the UI slider matches that 100%
        if (_volumeSlider != null)
        {
            _volumeSlider.value = 1f;
        }
        
    }
    
    // ── Unity lifecycle ────────────────────────────────────────────────────
    private void Awake()
    { 
        // Ensure this object survives scene transitions
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        } else {
            Destroy(gameObject);
            return;
        }

        _pim = GetComponent<PlayerInputManager>();
        
        // Fix join behaviour in player input component
        if (_pim.joinBehavior != PlayerJoinBehavior.JoinPlayersManually)
        {
            _pim.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
        }
    }

    private void OnEnable()
    {
        // Enable the Lobby action map and listen for Join presses
        // We use a single enabled InputActionAsset here (not attached to any PlayerInput)
        // purely to detect who is pressing Join and on which device.
        var lobbyMap = _lobbyInputAsset.FindActionMap("Lobby", throwIfNotFound: true);
        _joinAction = lobbyMap.FindAction("Join", throwIfNotFound: true);
        _joinAction.Enable();
        _joinAction.performed += OnJoinPerformed;

        _pim.onPlayerJoined += OnPlayerJoined;
        _pim.onPlayerLeft   += OnPlayerLeft;
    }

    private void OnDisable()
    {
        _joinAction.performed -= OnJoinPerformed;
        _joinAction.Disable();

        _pim.onPlayerJoined -= OnPlayerJoined;
        _pim.onPlayerLeft   -= OnPlayerLeft;
    }

    // ── Join detection ─────────────────────────────────────────────────────

    /// <summary>
    /// Called whenever any device presses the Join button.
    /// We inspect which device fired it, guard against duplicates,
    /// then call JoinPlayerFromActionIfNotAlreadyJoined.
    /// </summary>
    private void OnJoinPerformed(InputAction.CallbackContext ctx)
    {
        InputDevice device = ctx.control.device;

        // Already joined with this device?
        if (_activeGhosts.ContainsKey(device.deviceId))
        {
            Debug.Log($"[LobbyManager] Device {device.displayName} already joined.");
            return;
        }

        // Lobby full?
        if (_pim.playerCount >= _pim.maxPlayerCount)
        {
            Debug.Log("[LobbyManager] Lobby full.");
            return;
        }

        // Keyboard guard
        if (device is Keyboard && !_allowKeyboardWASD)
        {
            Debug.Log("[LobbyManager] Keyboard join disabled.");
            return;
        }

        // Determine the correct control scheme string for this device
        string scheme = ResolveControlScheme(device);
        if (scheme == null)
        {
            Debug.LogWarning($"[LobbyManager] No control scheme for device: {device.displayName}");
            return;
        }

        // Spawn a new PlayerInput (→ LobbyGhost) paired to this specific device + scheme
        // JoinPlayerFromActionIfNotAlreadyJoined ensures Unity's internal dedup too
        PlayerInput pi = PlayerInput.Instantiate(
            _ghostPrefab,
            controlScheme: scheme,
            pairWithDevice: device
        );

        if (pi == null)
        {
            Debug.LogError("[LobbyManager] JoinPlayer returned null — check prefab setup.");
        }
    }

    // ── PlayerInputManager callbacks ───────────────────────────────────────

    private void OnPlayerJoined(PlayerInput pi)
    {
        InputDevice device = pi.devices.Count > 0 ? pi.devices[0] : null;

        if (device != null)
            _activeGhosts[device.deviceId] = null; // placeholder until ghost registers

        LobbySlotUI freeSlot = GetFreeSlot();
        if (freeSlot == null)
        {
            Debug.LogError("[LobbyManager] No free slot for new player — this shouldn't happen.");
            return;
        }

        // LobbyGhost will call RegisterGhost on us from its Start()
        Debug.Log($"[LobbyManager] Player {pi.playerIndex} joined with {pi.currentControlScheme}");
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        var ghost = pi.GetComponent<LobbyGhost>();
        if (ghost != null) ghost.ReleaseSlot();

        InputDevice device = pi.devices.Count > 0 ? pi.devices[0] : null;
        if (device != null) _activeGhosts.Remove(device.deviceId);

        Debug.Log($"[LobbyManager] Player {pi.playerIndex} left.");
    }

    // ── Public API (called by LobbyGhost) ──────────────────────────────────

    /// <summary>Ghost calls this in Start() to claim a slot and register itself.</summary>
    public LobbySlotUI ClaimFreeSlot(LobbyGhost ghost, int deviceId)
    {
        LobbySlotUI slot = GetFreeSlot();
        if (slot == null) return null;

        slot.Claim(ghost);
        _activeGhosts[deviceId] = ghost;
        return slot;
    }

    /// <summary>
    /// Called by a Ghost when its player presses Ready.
    /// Checks if all present players are ready and triggers scene load.
    /// </summary>
    public void OnGhostReady(LobbyGhost ghost)
    {
        bool allReady = _activeGhosts.Values
            .Where(g => g != null)
            .All(g => g.IsReady);

        if (allReady && _activeGhosts.Count >= 1)
        {
            Debug.Log($"[Lobby] {ghost.PlayerIndex} is ready. Checking for Start Button...");
            
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private LobbySlotUI GetFreeSlot() =>
        _slots.FirstOrDefault(s => s != null && !s.IsClaimed);

    private string ResolveControlScheme(InputDevice device)
    {
        if (device is Keyboard) return SchemeKeyboard;
        if (device is Gamepad)
        {
            // If you have multiple gamepad schemes (Gamepad1, Gamepad2…) pick the unclaimed one.
            // For simplicity: check which gamepad schemes are already in use.
            var usedSchemes = _activeGhosts.Values
                .Where(g => g != null)
                .Select(g => g.ControlScheme)
                .ToHashSet();

            // Return the first Gamepad scheme not yet in use
            for (int i = 1; i <= 4; i++)
            {
                string candidate = $"Gamepad{i}";
                // If the scheme exists in the asset and isn't taken, use it
                var schemeDef = _lobbyInputAsset.FindControlScheme(candidate);
                if (schemeDef.HasValue && !usedSchemes.Contains(candidate))
                    return candidate;
            }

            // Fallback: asset uses a single "Gamepad" scheme
            return "Gamepad";
        }
        return null;
    }

    private void CommitAndLoad()
    {
        playersToSpawn.Clear();

        // 3. Save the current players into our simple list
        foreach (var ghost in _activeGhosts.Values.Where(g => g != null))
        {
            playersToSpawn.Add(new PlayerSelectionData
            {
                playerIndex = ghost.PlayerIndex,
                characterId = ghost.SelectedCharacterId,
                controlScheme = ghost.ControlScheme,
                deviceId = ghost.DeviceId,
            });
        }

        // SceneManager.LoadScene("LevelSelection");
        SceneManager.LoadScene(selectedLevelName);
    }
    
    public void SpawnAllPlayers(Transform spawnPoint)
    {
        foreach (var data in playersToSpawn)
        {
            InputDevice device = InputSystem.GetDeviceById(data.deviceId);
            PlayerInput pi = PlayerInput.Instantiate(_ghostPrefab, pairWithDevice: device, controlScheme: data.controlScheme);
            pi.transform.position = spawnPoint.position + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
        }
    }
    
// ── UI Logic ──────────────────────────────────────────────────────────

    public void OnStartButtonClicked()
    {
        // 1. Check if at least one player has joined
        if (_activeGhosts.Count == 0)
        {
            Debug.LogWarning("[Lobby] Cannot start: No players have joined!");
            return;
        }

        // 2. Only allow start if everyone who joined is 'Ready'
        bool allReady = _activeGhosts.Values
            .Where(g => g != null)
            .All(g => g.IsReady);

        if (allReady)
        {
            Debug.Log("[Lobby] Start Button pressed. Moving to Level Selection...");
            CommitAndLoad(); 
        }
        else
        {
            Debug.LogWarning("[Lobby] Cannot start: Someone is not ready yet!");
        }
    }
    
    // ── Settings Logic ──────────────────────────────────────────────
    public void SetVolume(float value)
    {
        // This sets the master volume (0.0 to 1.0)
        AudioListener.volume = value; 
        Debug.Log($"Volume set to: {value}");
    }
    
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    

    public void SetResolution(int index)
    {
        if (index == 0) Screen.SetResolution(1920, 1080, true);
        else if (index == 1) Screen.SetResolution(1280, 720, true);
        else if (index == 2) Screen.SetResolution(854, 480, true);
    }
    
}