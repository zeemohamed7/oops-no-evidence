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

    [HideInInspector] public List<PlayerSelectionData> playersToSpawn = new();
    public int currentLevelIndex = -1;

    private PlayerInputManager pim;
    private InputAction joinAction;
    private bool isTransitioning = false;
    private float lastJoinTime;
    private const float JoinCooldown = 0.1f;
    private readonly Dictionary<int, LobbyGhost> activeGhosts = new();
    private const string KeyboardScheme = "KeyboardWASD";

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
        pim.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
    }

    private void Start()
    {
        AudioListener.volume = 1f;
        if (volumeSlider != null) volumeSlider.value = 1f;

        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (warningPopupPanel) warningPopupPanel.SetActive(false);
    }
    
    private void OnEnable()
    {
        InputActionMap lobbyMap = lobbyInputAsset.FindActionMap("Lobby", true);
        joinAction = lobbyMap.FindAction("Join", true);
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
    // JOINING (FIXED DEVICE BLEEDING HERE)
    // ─────────────────────────────────────────────────────────────
    private void OnJoinPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || isTransitioning) return;
        if (Time.unscaledTime < lastJoinTime + JoinCooldown) return;

        InputDevice device = ctx.control.device;
        if (device == null || device.description.interfaceName == "Virtual") return;
        if (device is Keyboard && !allowKeyboard) return;
        if (activeGhosts.ContainsKey(device.deviceId)) return;

        if (activeGhosts.Count >= pim.maxPlayerCount)
        {
            Debug.Log("Lobby Full");
            return;
        }

        string scheme = ResolveControlScheme(device);
        if (string.IsNullOrEmpty(scheme))
        {
            Debug.LogWarning($"No scheme found for {device.displayName}");
            return;
        }

        activeGhosts.Add(device.deviceId, null);

        // FIXED: Explicitly pair device on birth to stop Player 3 input bleed
        PlayerInput pi = PlayerInput.Instantiate(
            ghostPrefab,
            pairWithDevice: device,
            controlScheme: scheme
        );

        if (pi == null)
        {
            activeGhosts.Remove(device.deviceId);
            return;
        }

        pi.neverAutoSwitchControlSchemes = true;
        pi.SwitchCurrentActionMap("Lobby");

        lastJoinTime = Time.unscaledTime;
    }

    private void OnPlayerJoined(PlayerInput pi)
    {
        if (isTransitioning) return;
        InputDevice device = pi.devices.Count > 0 ? pi.devices[0] : null;
        Debug.Log($"Player {pi.playerIndex} joined using {device?.displayName ?? "Unknown Device"}");
    }

    private void OnPlayerLeft(PlayerInput pi)
    {
        if (isTransitioning) return;

        InputDevice device = pi.devices.Count > 0 ? pi.devices[0] : null;
        if (device != null) activeGhosts.Remove(device.deviceId);

        LobbyGhost ghost = pi.GetComponent<LobbyGhost>();
        if (ghost != null) ghost.ReleaseSlot();

        Debug.Log($"Player {pi.playerIndex} left");
    }

    public LobbySlotUI ClaimFreeSlot(LobbyGhost ghost, int deviceId)
    {
        LobbySlotUI slot = slots.FirstOrDefault(s => s != null && !s.IsClaimed);
        if (slot == null) return null;

        slot.Claim(ghost);
        activeGhosts[deviceId] = ghost;
        return slot;
    }

    public void OnGhostReady(LobbyGhost ghost)
    {
        bool allReady = activeGhosts.Values.Where(g => g != null).All(g => g.IsReady);
        if (allReady && activeGhosts.Count > 0)
        {
            Debug.Log("All players ready");
        }
    }

    public void OnStartButtonClicked()
    {
        if (activeGhosts.Count == 0)
        {   
            ShowWarningPopup("No players have joined!");
            return;
        }

        bool allReady = activeGhosts.Values.Where(g => g != null).All(g => g.IsReady);
        if (!allReady)
        {
            ShowWarningPopup("Not everyone is ready!");
            return;
        }

        CommitAndLoad();
    }

    private void ShowWarningPopup(string message)
    {
        if (readyForEveryone != null && warningPopupPanel != null)
        {
            readyForEveryone.text = message;
            StopAllCoroutines(); 
            StartCoroutine(FadeWarningWindow());
        }
    }

    private IEnumerator FadeWarningWindow()
    {
        CanvasGroup canvasGroup = warningPopupPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        warningPopupPanel.SetActive(true);
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(2.0f); 

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

    // ─────────────────────────────────────────────────────────────
    // SCENE TRANSITION (FIXED MOVEMENT LOCK-UP HERE)
    // ─────────────────────────────────────────────────────────────
    private void CommitAndLoad()
    {
        isTransitioning = true;
        playersToSpawn.Clear();

        foreach (LobbyGhost ghost in activeGhosts.Values)
        {
            if (ghost == null) continue;

            playersToSpawn.Add(new PlayerSelectionData
            {
                playerIndex = ghost.PlayerIndex,
                characterId = ghost.SelectedCharacterId,
                controlScheme = ghost.ControlScheme,
                deviceId = ghost.DeviceId
            });
        }

        // FIXED: Do not toggle .enabled = false on the component itself!
        if (pim != null)
        {
            pim.DisableJoining();
        }

        pim.onPlayerJoined -= OnPlayerJoined;
        pim.onPlayerLeft -= OnPlayerLeft;

        SceneManager.LoadScene(selectedLevelName);
    }

    public string GetSelectedLevelName() => selectedLevelName;
    public void SetCurrentLevel(int levelIndex) => currentLevelIndex = levelIndex;

    // ─────────────────────────────────────────────────────────────
    // SPAWN PLAYERS (FIXED SYNCHRONOUS INITIALIZATION HERE)
    // ─────────────────────────────────────────────────────────────
    // public void SpawnAllPlayers(Transform spawnPoint)
    // {
    //     if (playersToSpawn.Count == 0) return;
    //
    //     int index = 0;
    //     Vector3 camForward = Camera.main.transform.forward;
    //     camForward.y = 0;
    //     camForward.Normalize();
    //
    //     foreach (PlayerSelectionData data in playersToSpawn)
    //     {
    //         GameObject prefabToSpawn = ghostPrefab;
    //         
    //         foreach (CharacterMap map in characterPrefabs)
    //         {
    //             if (map.id == data.characterId) { prefabToSpawn = map.prefab; break; }
    //         }
    //
    //         InputDevice device = InputSystem.GetDeviceById(data.deviceId);
    //         
    //         PlayerInput pi = PlayerInput.Instantiate(
    //             prefabToSpawn, 
    //             pairWithDevice: device, 
    //             controlScheme: data.controlScheme
    //         );
    //
    //         if (pi != null)
    //         {
    //             // Force messaging notification channel setup
    //             pi.notificationBehavior = PlayerNotifications.SendMessages;
    //
    //             // Strip the lobby tracking logic completely
    //             LobbyGhost lg = pi.GetComponent<LobbyGhost>();
    //             if (lg != null) Destroy(lg);
    //
    //             // Set maps synchronously right here before the movement scripts wake up
    //             pi.neverAutoSwitchControlSchemes = true;
    //             pi.SwitchCurrentActionMap("Player");
    //             pi.camera = Camera.main;
    //
    //             CharacterController cc = pi.GetComponent<CharacterController>();
    //             TopDownPlayerController moveScript = pi.GetComponent<TopDownPlayerController>();
    //
    //             if (cc != null) cc.enabled = false;
    //             if (moveScript != null) moveScript.enabled = false;
    //
    //             float xOffset = (index - (playersToSpawn.Count - 1) / 2f) * 1.5f;
    //             pi.transform.position = spawnPoint.position + (Camera.main.transform.right * xOffset);
    //             pi.transform.rotation = Quaternion.LookRotation(camForward);
    //
    //             if (cc != null) cc.enabled = true;
    //             if (moveScript != null) moveScript.enabled = true; 
    //
    //             index++;
    //         }
    //     }
    // }

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
        
        // 1. Instantiate using Unity's factory method
        PlayerInput pi = PlayerInput.Instantiate(
            prefabToSpawn, 
            pairWithDevice: device, 
            controlScheme: data.controlScheme
        );

        if (pi != null)
        {
            // 2. Clear out any lingering lobby tracking scripts
            LobbyGhost lg = pi.GetComponentInChildren<LobbyGhost>();
            if (lg != null) Destroy(lg);

            // 3. Force the notification behavior and action map configurations
            pi.notificationBehavior = PlayerNotifications.SendMessages;
            pi.neverAutoSwitchControlSchemes = true;
            pi.SwitchCurrentActionMap("Player");
            pi.camera = Camera.main;

            // 4. Look for the movement scripts on the root OR in the children objects
            CharacterController cc = pi.GetComponentInChildren<CharacterController>();
            TopDownPlayerController moveScript = pi.GetComponentInChildren<TopDownPlayerController>();

            // 5. Temporarily disable physics engines while updating placement coordinates
            if (cc != null) cc.enabled = false;
            if (moveScript != null) moveScript.enabled = false;

            // Apply offsets so multiple players don't spawn inside each other
            float xOffset = (index - (playersToSpawn.Count - 1) / 2f) * 1.5f;
            pi.transform.position = spawnPoint.position + (Camera.main.transform.right * xOffset);
            pi.transform.rotation = Quaternion.LookRotation(camForward);

            // 6. Reactivate the character movement systems
            if (cc != null) cc.enabled = true;
            if (moveScript != null) moveScript.enabled = true; 

            index++;
        }
    }
}
    private string ResolveControlScheme(InputDevice device)
    {
        if (device is Keyboard) return KeyboardScheme;

        if (device is Gamepad)
        {
            // Count how many controllers there are already
            int currentGamepadCount = activeGhosts.Values.Count(g => g != null && g.ControlScheme.StartsWith("Gamepad"));
            int dynamicGamepadIndex = currentGamepadCount + 1;

            string scheme = $"Gamepad{dynamicGamepadIndex}";
        
            // Match the scheme from inputactionsystem 
            var found = lobbyInputAsset.FindControlScheme(scheme);
            if (found.HasValue) 
            {
                return scheme;
            }

            // Deleted Gamepad cause it was causing issues so nvm
            // // Fallback safety shield if your Input Action asset asset profile is missing a numbered slot
            // Debug.LogWarning($"[SCHEME FALLBACK] '{scheme}' wasn't found in your Input Action Asset. Defaulting to generic 'Gamepad'.");
            // return "Gamepad";
            return "Gamepad1";
        }
        return null;
    }
    public void OpenCredits() => SwitchPanel(creditsPanel);
    public void OpenSettings() => SwitchPanel(settingsPanel);
    public void BackToMain() => SwitchPanel(mainMenuPanel);

    private void SwitchPanel(GameObject target)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (target) target.SetActive(true);
    }

    public void SetVolume(float value) => AudioListener.volume = value;
    public void SetFullscreen(bool fullscreen) => Screen.fullScreen = fullscreen;

    public void SetResolution(int index)
    {
        if (index == 0) Screen.SetResolution(1920, 1080, true);
        if (index == 1) Screen.SetResolution(1280, 720, true);
        if (index == 2) Screen.SetResolution(854, 480, true);
    }
}