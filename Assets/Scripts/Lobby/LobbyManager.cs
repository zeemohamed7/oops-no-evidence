using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
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
    [SerializeField] private TextMeshProUGUI readyForEveryone;
    

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

    // Persistent player data
    public List<PlayerSelectionData> playersToSpawn = new();

    // Private
    private PlayerInputManager pim;
    private InputAction joinAction;

    private bool isTransitioning = false;

    private float lastJoinTime;
    private const float JoinCooldown = 0.1f;

    // deviceId -> LobbyGhost
    private readonly Dictionary<int, LobbyGhost> activeGhosts = new();

    // Control schemes
    private const string KeyboardScheme = "KeyboardWASD";

    // ─────────────────────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────────────────────

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

        pim = GetComponent<PlayerInputManager>();

        // MUST BE MANUAL
        pim.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
    }

    private void Start()
    {
        AudioListener.volume = 1f;

        if (volumeSlider != null)
            volumeSlider.value = 1f;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (warningPopupPanel != null)
            warningPopupPanel.SetActive(false);
    }

    private void OnEnable()
    {
        // GLOBAL JOIN LISTENER
        // This is the ONLY thing listening for Join

        InputActionMap lobbyMap =
            lobbyInputAsset.FindActionMap("Lobby", true);

        joinAction =
            lobbyMap.FindAction("Join", true);

        joinAction.Enable();
        joinAction.performed += OnJoinPerformed;

        pim.onPlayerJoined += OnPlayerJoined;
        pim.onPlayerLeft += OnPlayerLeft;
    }

    private void OnDisable()
    {
        if (joinAction != null)
        {
            joinAction.performed -= OnJoinPerformed;
            joinAction.Disable();
        }

        pim.onPlayerJoined -= OnPlayerJoined;
        pim.onPlayerLeft -= OnPlayerLeft;
    }

    // ─────────────────────────────────────────────────────────────
    // JOINING
    // ─────────────────────────────────────────────────────────────

    private void OnJoinPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (isTransitioning) return;

        // Debounce
        if (Time.unscaledTime < lastJoinTime + JoinCooldown)
            return;

        InputDevice device = ctx.control.device;

        if (device == null)
            return;

        // Ignore virtual devices
        if (device.description.interfaceName == "Virtual")
            return;

        // Keyboard disabled?
        if (device is Keyboard && !allowKeyboard)
            return;

        // Already joined?
        if (activeGhosts.ContainsKey(device.deviceId))
            return;

        // Lobby full?
        if (activeGhosts.Count >= pim.maxPlayerCount)
        {
            Debug.Log("Lobby Full");
            return;
        }

        string scheme = ResolveControlScheme(device);

        if (string.IsNullOrEmpty(scheme))
        {
            Debug.LogWarning($"No scheme for {device.displayName}");
            return;
        }

        // LOCK DEVICE IMMEDIATELY
        activeGhosts.Add(device.deviceId, null);
        GameObject obj = Instantiate(ghostPrefab);

        PlayerInput pi = obj.GetComponent<PlayerInput>();

        pi.user.UnpairDevices();

        InputUser.PerformPairingWithDevice(
            device,
            pi.user
        );

        Debug.Log(
            $"PLAYER {pi.playerIndex} PAIRED TO: " +
            string.Join(", ", pi.devices)
        );
        
        pi.SwitchCurrentActionMap("Player");


        if (pi == null)
        {
            activeGhosts.Remove(device.deviceId);
            Debug.LogError("Failed to create player");
            return;
        }

        lastJoinTime = Time.unscaledTime;

        Debug.Log($"Joined: {device.displayName}");
    }

    // ─────────────────────────────────────────────────────────────
    // PLAYER CALLBACKS
    // ─────────────────────────────────────────────────────────────

    private void OnPlayerJoined(PlayerInput pi)
    {
        if (isTransitioning) return;

        InputDevice device =
            pi.devices.Count > 0
            ? pi.devices[0]
            : null;

        if (device == null)
        {
            Debug.LogWarning("Joined with no device");
            return;
        }

        Debug.Log(
            $"Player {pi.playerIndex} joined " +
            $"using {device.displayName}"
        );
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        if (isTransitioning) return;

        InputDevice device =
            pi.devices.Count > 0
            ? pi.devices[0]
            : null;

        if (device != null)
        {
            activeGhosts.Remove(device.deviceId);
        }

        LobbyGhost ghost = pi.GetComponent<LobbyGhost>();

        if (ghost != null)
        {
            ghost.ReleaseSlot();
        }

        Debug.Log($"Player {pi.playerIndex} left");
    }

    // ─────────────────────────────────────────────────────────────
    // SLOT CLAIMING
    // ─────────────────────────────────────────────────────────────

    public LobbySlotUI ClaimFreeSlot(
        LobbyGhost ghost,
        int deviceId
    )
    {
        LobbySlotUI slot =
            slots.FirstOrDefault(s => s != null && !s.IsClaimed);

        if (slot == null)
            return null;

        slot.Claim(ghost);

        activeGhosts[deviceId] = ghost;

        return slot;
    }

    // ─────────────────────────────────────────────────────────────
    // READY CHECK
    // ─────────────────────────────────────────────────────────────

    public void OnGhostReady(LobbyGhost ghost)
    {
        bool allReady = activeGhosts.Values
            .Where(g => g != null)
            .All(g => g.IsReady);

        if (allReady && activeGhosts.Count > 0)
        {
            Debug.Log("All players ready");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // START GAME
    // ─────────────────────────────────────────────────────────────

    public void OnStartButtonClicked()
    {
        if (activeGhosts.Count == 0)
        {   
            ShowWarningPopup("No players have joined!");
            Debug.LogWarning("No players joined");
            return;
        }

        bool allReady = activeGhosts.Values
            .Where(g => g != null)
            .All(g => g.IsReady);

        if (!allReady)
        {
            ShowWarningPopup("Not everyone is ready!");
            Debug.LogWarning("Not everyone ready");
            return;
        }

        CommitAndLoad();
    }

    private void ShowWarningPopup(string message)
    {
        if (readyForEveryone != null && warningPopupPanel != null)
        {
            readyForEveryone.text = message;
            
            // Stop any active fade routines so they don't overlap
            StopAllCoroutines(); 
            StartCoroutine(FadeWarningWindow());
        }
    }

    private IEnumerator FadeWarningWindow()
    {
        CanvasGroup canvasGroup = warningPopupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        warningPopupPanel.SetActive(true);
        float duration = 0.4f; // How fast to fade in/out (in seconds)
        float elapsed = 0f;

        // 1. FADE IN
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. WAIT ON SCREEN
        yield return new WaitForSecondsRealtime(2.0f); 

        // 3. FADE OUT
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        warningPopupPanel.SetActive(false);
    }

    // Coroutine that handles the visual display timer
    private IEnumerator FlashWarningWindow()
    {
        warningPopupPanel.SetActive(true);
        
        // Wait on screen for 2.5 seconds
        yield return new WaitForSecondsRealtime(2.5f); 
        
        warningPopupPanel.SetActive(false);
    }
    
    private void CommitAndLoad()
    {
        isTransitioning = true;

        playersToSpawn.Clear();

        foreach (LobbyGhost ghost in activeGhosts.Values)
        {
            if (ghost == null)
                continue;

            playersToSpawn.Add(new PlayerSelectionData
            {
                playerIndex = ghost.PlayerIndex,
                characterId = ghost.SelectedCharacterId,
                controlScheme = ghost.ControlScheme,
                deviceId = ghost.DeviceId
            });
        }

        PlayerInputManager.instance.DisableJoining();
        PlayerInputManager.instance.enabled = false;

        pim.onPlayerJoined -= OnPlayerJoined;
        pim.onPlayerLeft -= OnPlayerLeft;

        SceneManager.LoadScene(selectedLevelName);
    }
    public string GetSelectedLevelName()
    {
        return selectedLevelName;
    }

    // Set by LevelMenuManager when the player picks a level on the overworld map
    public int currentLevelIndex = -1;

    public void SetCurrentLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
    }
    // ─────────────────────────────────────────────────────────────
    // SPAWN PLAYERS
    // ─────────────────────────────────────────────────────────────

    private IEnumerator DelayedMapSwitch(PlayerInput pi)
    {
        yield return null;

        pi.SwitchCurrentActionMap("Player");
    }
    
    public void SpawnAllPlayers(Transform spawnPoint)
    {
        
        if (playersToSpawn.Count == 0) return;

        int index = 0;
        Vector3 camForward = Camera.main.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        foreach (PlayerSelectionData data in playersToSpawn)
        {
            GameObject prefabToSpawn = ghostPrefab;
            
            foreach (CharacterMap map in characterPrefabs)
            {
                if (map.id == data.characterId) { prefabToSpawn = map.prefab; break; }
            }

            InputDevice device = InputSystem.GetDeviceById(data.deviceId);
            PlayerInput pi = PlayerInput.Instantiate(prefabToSpawn, pairWithDevice: device, controlScheme: data.controlScheme);

            if (pi != null)
            {
                // 1. Get both the Unity controller and YOUR movement script
                CharacterController cc = pi.GetComponent<CharacterController>();
                TopDownPlayerController moveScript = pi.GetComponent<TopDownPlayerController>();

                // 2. Disable both for positioning
                if (cc != null) cc.enabled = false;
                if (moveScript != null) moveScript.enabled = false;

                float xOffset = (index - (playersToSpawn.Count - 1) / 2f) * 1.5f;
                pi.transform.position = spawnPoint.position + (Camera.main.transform.right * xOffset);
                pi.transform.rotation = Quaternion.LookRotation(camForward);

                // 3. RE-ENABLE BOTH (This overrides the LobbyGhost Awake logic)
                if (cc != null) cc.enabled = true;
                if (moveScript != null) moveScript.enabled = true; 

                // 4. Force the gameplay map
                pi.camera = Camera.main;
                StartCoroutine(DelayedMapSwitch(pi));
                pi.neverAutoSwitchControlSchemes = true;

                // 5. Kill the lobby logic so it stops interfering
                LobbyGhost lg = pi.GetComponent<LobbyGhost>();
                if (lg != null) lg.enabled = false;

                index++;
            }
        }
        
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────

    private string ResolveControlScheme(InputDevice device)
    {
        if (device is Keyboard)
            return KeyboardScheme;

        if (device is Gamepad)
        {
            for (int i = 1; i <= 4; i++)
            {
                string scheme = $"Gamepad{i}";

                var found =
                    lobbyInputAsset.FindControlScheme(scheme);

                if (found.HasValue)
                    return scheme;
            }

            return "Gamepad";
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────────
    // MENU UI
    // ─────────────────────────────────────────────────────────────

    public void OpenCredits()
    {
        SwitchPanel(creditsPanel);
    }

    public void OpenSettings()
    {
        SwitchPanel(settingsPanel);
    }

    public void BackToMain()
    {
        SwitchPanel(mainMenuPanel);
    }

    private void SwitchPanel(GameObject target)
    {
        if (mainMenuPanel)
            mainMenuPanel.SetActive(false);

        if (creditsPanel)
            creditsPanel.SetActive(false);

        if (settingsPanel)
            settingsPanel.SetActive(false);

        if (target)
            target.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────────────────────

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }

    public void SetResolution(int index)
    {
        if (index == 0)
            Screen.SetResolution(1920, 1080, true);

        if (index == 1)
            Screen.SetResolution(1280, 720, true);

        if (index == 2)
            Screen.SetResolution(854, 480, true);
    }
}