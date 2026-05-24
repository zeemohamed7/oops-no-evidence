using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerInputManager))]
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject warningPopupPanel;
    [SerializeField] private TextMeshProUGUI warningText;

    [Header("Input")]
    [SerializeField] private InputActionAsset lobbyInputAsset;

    [Header("Lobby")]
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private List<LobbySlotUI> slots = new();

    [Header("Scene")]
    [SerializeField] private string selectedLevelName = "LevelSelection";

    [Header("Settings")]
    [SerializeField] private bool allowKeyboard = true;
    [SerializeField] private Slider volumeSlider;

    [System.Serializable]
    public struct CharacterMap
    {
        public string id;
        public GameObject prefab;
    }

    [Header("Characters")]
    public List<CharacterMap> characterPrefabs = new();

    // Exposed to the spawner in the next scene
    [HideInInspector] public List<PlayerSelectionData> playersToSpawn = new();
    public int currentLevelIndex = -1;

    private PlayerInputManager _pim;
    private InputAction _joinAction;
    private bool _isTransitioning = false;
    private float _lastJoinTime;
    private const float JoinCooldown = 0.15f;
    private const string KeyboardScheme = "Keyboard&Mouse";

    // Keyed by hardware deviceId — the single source of truth for who's in the lobby
    private readonly Dictionary<int, LobbyGhost> _activeGhosts = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _pim = GetComponent<PlayerInputManager>();
        _pim.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
    }

    private void Start()
    {
        AudioListener.volume = 1f;
        if (volumeSlider != null) volumeSlider.value = 1f;

        if (mainMenuPanel)      mainMenuPanel.SetActive(true);
        if (creditsPanel)       creditsPanel.SetActive(false);
        if (settingsPanel)      settingsPanel.SetActive(false);
        if (warningPopupPanel)  warningPopupPanel.SetActive(false);
    }

    private void OnEnable()
    {
        InputActionMap lobbyMap = lobbyInputAsset.FindActionMap("Lobby", true);
        _joinAction = lobbyMap.FindAction("Join", true);
        _joinAction.Enable();
        _joinAction.performed += OnJoinPerformed;

        _pim.onPlayerJoined += OnPlayerJoined;
        _pim.onPlayerLeft   += OnPlayerLeft;
    }

    private void OnDisable()
    {
        if (_joinAction != null)
        {
            _joinAction.performed -= OnJoinPerformed;
            _joinAction.Disable();
        }

        _pim.onPlayerJoined -= OnPlayerJoined;
        _pim.onPlayerLeft   -= OnPlayerLeft;
    }

    // ─── JOIN GATE ────────────────────────────────────────────────────────────
    // All joining is triggered by the shared Lobby/Join action. We resolve the
    // exact device that fired it and build an isolated PlayerInput from there.
    // Nothing downstream ever looks at Keyboard.current or Gamepad.current.

    private void OnJoinPerformed(InputAction.CallbackContext ctx)
    {
        if (_isTransitioning) return;
        if (Time.unscaledTime < _lastJoinTime + JoinCooldown) return;

        InputDevice device = ctx.control.device;
        if (device == null || device.description.interfaceName == "Virtual") return;
        if (device is Keyboard && !allowKeyboard) return;

        // Already in the lobby — ignore repeat presses
        if (_activeGhosts.ContainsKey(device.deviceId)) return;

        if (_activeGhosts.Count >= _pim.maxPlayerCount)
            return;

        string scheme = ResolveControlScheme(device);
        if (string.IsNullOrEmpty(scheme))
            return;

        // Reserve the slot before instantiation so a second rapid press can't race in
        _activeGhosts[device.deviceId] = null;

        // playerIndex is computed explicitly — Unity must not auto-assign index 0 to everyone
        int playerIndex = _activeGhosts.Count - 1;

        PlayerInput pi = PlayerInput.Instantiate(
            ghostPrefab,
            playerIndex: playerIndex,
            controlScheme: scheme,
            pairWithDevice: device
        );

        if (pi == null)
        {
            _activeGhosts.Remove(device.deviceId);
            return;
        }

        // Lock this player to exactly one device for its entire lifecycle
        pi.neverAutoSwitchControlSchemes = true;
        pi.SwitchCurrentActionMap("LobbyUI");

        _lastJoinTime = Time.unscaledTime;
    }

    // ─── PLAYER JOIN / LEAVE CALLBACKS ───────────────────────────────────────

    private void OnPlayerJoined(PlayerInput pi)
    {
        if (_isTransitioning) return;
        // LobbyGhost.Start() calls ClaimFreeSlot, so nothing extra needed here
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        if (_isTransitioning) return;

        InputDevice device = pi.devices.Count > 0 ? pi.devices[0] : null;
        if (device != null) _activeGhosts.Remove(device.deviceId);

        pi.GetComponent<LobbyGhost>()?.ReleaseSlot();
    }

    // ─── SLOT MANAGEMENT (called by LobbyGhost on Start) ─────────────────────

    public LobbySlotUI ClaimFreeSlot(LobbyGhost ghost, int deviceId)
    {
        LobbySlotUI slot = slots.FirstOrDefault(s => s != null && !s.IsClaimed);
        if (slot == null) return null;

        slot.Claim(ghost);
        _activeGhosts[deviceId] = ghost;
        return slot;
    }

    // ─── READY CHECK ──────────────────────────────────────────────────────────

    public void OnGhostReady(LobbyGhost ghost)
    {
        // Called by each ghost when it flips ready — no action needed here yet,
        // but this hook is useful for future "countdown" or "auto-start" logic
    }

    // ─── START BUTTON ─────────────────────────────────────────────────────────

    public void OnStartButtonClicked()
    {
        if (_activeGhosts.Count < 2)
        {
            ShowWarningPopup("This game needs at least 2 players!");
            return;
        }

        bool allReady = _activeGhosts.Values.Where(g => g != null).All(g => g.IsReady);
        if (!allReady)
        {
            ShowWarningPopup("Not everyone is ready!");
            return;
        }

        CommitAndLoad();
    }

    // ─── WARNING POPUP ────────────────────────────────────────────────────────

    private void ShowWarningPopup(string message)
    {
        if (warningText == null || warningPopupPanel == null) return;
        warningText.text = message;
        StopAllCoroutines();
        StartCoroutine(FadeWarningWindow());
    }

    private IEnumerator FadeWarningWindow()
    {
        CanvasGroup cg = warningPopupPanel.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        warningPopupPanel.SetActive(true);
        float duration = 0.4f;

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(2f);

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        warningPopupPanel.SetActive(false);
    }

    // ─── SCENE TRANSITION ─────────────────────────────────────────────────────

    private void CommitAndLoad()
    {
        _isTransitioning = true;
        playersToSpawn.Clear();

        foreach (LobbyGhost ghost in _activeGhosts.Values.Where(g => g != null))
        {
            playersToSpawn.Add(new PlayerSelectionData
            {
                playerIndex   = ghost.PlayerIndex,
                characterId   = ghost.SelectedCharacterId,
                controlScheme = ghost.ControlScheme,
                deviceId      = ghost.DeviceId
            });
        }

        // Shut down joining cleanly — never disable the component itself
        _pim.DisableJoining();
        _pim.onPlayerJoined -= OnPlayerJoined;
        _pim.onPlayerLeft   -= OnPlayerLeft;

        SceneManager.LoadScene(selectedLevelName);
    }

    // ─── GAMEPLAY SPAWN (called by level spawner after scene load) ────────────

    public void SpawnAllPlayers(Transform spawnPoint)
    {
        if (playersToSpawn.Count == 0) return;

        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        for (int i = 0; i < playersToSpawn.Count; i++)
        {
            PlayerSelectionData data = playersToSpawn[i];

            GameObject prefab = characterPrefabs.FirstOrDefault(m => m.id == data.characterId).prefab
                                ?? ghostPrefab;

            InputDevice device = InputSystem.GetDeviceById(data.deviceId);

            PlayerInput pi = PlayerInput.Instantiate(
                prefab,
                playerIndex: data.playerIndex,
                controlScheme: data.controlScheme,
                pairWithDevice: device
            );

            if (pi == null) continue;

            // Strip any lingering lobby logic from the spawned prefab
            LobbyGhost lg = pi.GetComponentInChildren<LobbyGhost>();
            if (lg != null) Destroy(lg);

            // Lock the device and switch to gameplay actions before any script wakes up
            pi.neverAutoSwitchControlSchemes = true;
            pi.notificationBehavior = PlayerNotifications.SendMessages;
            pi.SwitchCurrentActionMap("Player");
            pi.camera = Camera.main;

            // Disable physics while we place the character, then re-enable
            CharacterController cc     = pi.GetComponentInChildren<CharacterController>();
            TopDownPlayerController mv = pi.GetComponentInChildren<TopDownPlayerController>();

            if (cc) cc.enabled = false;
            if (mv) mv.enabled = false;

            float xOffset = (i - (playersToSpawn.Count - 1) / 2f) * 1.5f;
            pi.transform.position = spawnPoint.position + Camera.main.transform.right * xOffset;
            pi.transform.rotation = Quaternion.LookRotation(camForward);

            if (cc) cc.enabled = true;
            if (mv) mv.enabled = true;
        }
    }

    // ─── CONTROL SCHEME RESOLUTION ────────────────────────────────────────────
    // Maps hardware to the named scheme in the Input Action Asset.
    // Gamepad slots are numbered (Gamepad1, Gamepad2…) to keep bindings isolated.

    private string ResolveControlScheme(InputDevice device)
    {
        if (device is Keyboard)
            return KeyboardScheme;

        if (device is Gamepad)
        {
            // Count only confirmed (non-null) gamepad ghosts already registered
            int gamepadCount = _activeGhosts.Values
                .Count(g => g != null && g.ControlScheme.StartsWith("Gamepad"));

            string scheme = $"Gamepad{gamepadCount + 1}";
            return lobbyInputAsset.FindControlScheme(scheme).HasValue ? scheme : "Gamepad1";
        }

        return null;
    }

    // ─── UTILITY ──────────────────────────────────────────────────────────────

    public string GetSelectedLevelName() => selectedLevelName;
    public void SetCurrentLevel(int index) => currentLevelIndex = index;

    public void OpenCredits()  => SwitchPanel(creditsPanel);
    public void OpenSettings() => SwitchPanel(settingsPanel);
    public void BackToMain()   => SwitchPanel(mainMenuPanel);

    private void SwitchPanel(GameObject target)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (creditsPanel)  creditsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (target)        target.SetActive(true);
    }

    public void SetVolume(float value)             => AudioListener.volume = value;
    public void SetFullscreen(bool fullscreen)     => Screen.fullScreen = fullscreen;

    public void SetResolution(int index)
    {
        var resolutions = new[] { (1920, 1080), (1280, 720), (854, 480) };
        if (index >= 0 && index < resolutions.Length)
            Screen.SetResolution(resolutions[index].Item1, resolutions[index].Item2, true);
    }
}
