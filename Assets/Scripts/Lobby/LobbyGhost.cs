using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class LobbyGhost : MonoBehaviour
{
    private static readonly List<string> Characters = new()
    {
        "Zainab", "Hajar", "Maryam", "Malak", "Noora"
    };

    private PlayerInput _playerInput;
    private LobbySlotUI _claimedSlot;
    private LobbyManager _lobbyManager;
    private int _characterIndex = 0;
    private bool _isReady = false;

    private float _navCooldown = 0f;
    private const float NavCooldownTime = 0.25f;

    public int PlayerIndex => _playerInput.playerIndex;
    public string SelectedCharacterId => Characters[_characterIndex];
    public string ControlScheme => _playerInput.currentControlScheme;

    public int DeviceId => _playerInput.devices.Count > 0
        ? _playerInput.devices[0].deviceId
        : -1;

    public bool IsReady => _isReady;

    private void Awake()
    {
        // 1. Switch the map to "Lobby" so this ghost doesn't use Gameplay actions
        var pInput = GetComponent<PlayerInput>();
        if (pInput != null) pInput.SwitchCurrentActionMap("Lobby");
        _playerInput = GetComponent<PlayerInput>();
        _lobbyManager = FindFirstObjectByType<LobbyManager>();

        // 2. DISABLE the movement script so it stops calling HandleMovement()
        var controllerScript = GetComponent<TopDownPlayerController>(); 
        if (controllerScript != null) 
        {
            controllerScript.enabled = false;
            Debug.Log("TopDownPlayerController disabled for the Lobby!");
        }
    }
    private void Start()
    {
        if (_lobbyManager == null)
        {
            Debug.LogError("[LobbyGhost] Could not find LobbyManager in scene!");
            return;
        }

        _claimedSlot = _lobbyManager.ClaimFreeSlot(this, DeviceId);

        if (_claimedSlot == null)
        {
            Debug.LogError("[LobbyGhost] No free slot available — destroying ghost.");
            Destroy(gameObject);
            return;
        }

        _claimedSlot.Initialize(PlayerIndex);
        _claimedSlot.SetCharacter(SelectedCharacterId, _characterIndex, Characters.Count);
        _claimedSlot.SetReady(false);

        Debug.Log($"[LobbyGhost] P{PlayerIndex} claimed slot '{_claimedSlot.name}' " +
                  $"with scheme '{ControlScheme}'");
    }

    private void Update()
    {
        if (_navCooldown > 0f) _navCooldown -= Time.deltaTime;
    }

    private void OnDestroy()
    {
        ReleaseSlot();
    }

    public void ReleaseSlot()
    {
        if (_claimedSlot != null)
        {
            _claimedSlot.Release();
            _claimedSlot = null;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // INPUTVALUE VERSION
    // ─────────────────────────────────────────────────────────────

    public void OnNavigate(InputValue value)
    {
        if (_isReady) return;
        if (_navCooldown > 0f) return;

        Vector2 dir = value.Get<Vector2>();

        if (Mathf.Abs(dir.x) > 0.5f)
        {
            int direction = dir.x > 0 ? 1 : -1;

            _characterIndex =
                (_characterIndex + direction + Characters.Count) % Characters.Count;

            _navCooldown = NavCooldownTime;


            _claimedSlot?.SetCharacter(
                SelectedCharacterId,
                _characterIndex,
                Characters.Count
            );
        }
    }

    public void OnSelect(InputValue value)
    {
        if (!value.isPressed) return;

        // Reserved for future confirm / inspect behaviour
        Debug.Log($"[LobbyGhost] P{PlayerIndex} Select pressed");
    }

    public void OnBack(InputValue value)
    {
        if (!value.isPressed) return;

        if (_isReady)
        {
            _isReady = false;
            _claimedSlot?.SetReady(false);

            Debug.Log($"[LobbyGhost] P{PlayerIndex} un-readied.");
        }
    }

    public void OnReady(InputValue value)
    {
        if (!value.isPressed) return;
        if (_claimedSlot == null) return;

        _isReady = !_isReady;
        _claimedSlot.SetReady(_isReady);

        Debug.Log($"[LobbyGhost] P{PlayerIndex} ready state: {_isReady} " +
                  $"| Character: {SelectedCharacterId}");

        if (_isReady)
        {
            _lobbyManager?.OnGhostReady(this);
        }
    }
}