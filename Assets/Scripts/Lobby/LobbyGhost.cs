using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Represents one player's presence in the lobby. Owns exactly one InputDevice.
/// All input arrives through the PlayerInput message system — never via global polling.
/// </summary>
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
    private const float NavCooldownTime = 0.2f;

    // ─── Public accessors (read by LobbyManager) ──────────────────────────────

    public int    PlayerIndex        => _playerInput.playerIndex;
    public string SelectedCharacterId => Characters[_characterIndex];
    public string ControlScheme      => _playerInput.currentControlScheme;
    public int    DeviceId           => _playerInput.devices.Count > 0 ? _playerInput.devices[0].deviceId : -1;
    public bool   IsReady            => _isReady;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        _playerInput  = GetComponent<PlayerInput>();
        _lobbyManager = LobbyManager.Instance;
    }

    private void Start()
    {
        // Ghosts that accidentally survive into the gameplay scene should self-disable
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            enabled = false;
            return;
        }

        // Ensure the action map is correct regardless of what the prefab defaulted to
        _playerInput.SwitchCurrentActionMap("LobbyUI");

        if (_lobbyManager == null) return;

        _claimedSlot = _lobbyManager.ClaimFreeSlot(this, DeviceId);

        // No free slot means the lobby is somehow full — remove ourselves cleanly
        if (_claimedSlot == null)
        {
            Destroy(gameObject);
            return;
        }

        _claimedSlot.Initialize(PlayerIndex);
        _claimedSlot.SetCharacter(SelectedCharacterId, _characterIndex, Characters.Count);
        _claimedSlot.SetReady(false);
    }

    private void Update()
    {
        if (_navCooldown > 0f)
            _navCooldown -= Time.deltaTime;
    }

    private void OnDestroy()
    {
        ReleaseSlot();
    }

    // ─── Slot cleanup (also called by LobbyManager.OnPlayerLeft) ─────────────

    public void ReleaseSlot()
    {
        if (_claimedSlot == null) return;
        _claimedSlot.Release();
        _claimedSlot = null;
    }

    // ─── Input message receivers ──────────────────────────────────────────────
    // Unity's PlayerInput sends these as messages to this GameObject only.
    // No other ghost will ever receive them — device isolation is guaranteed
    // by the explicit pairWithDevice used at Instantiate time.

    public void OnNavigate(InputValue value)
    {
        if (_isReady || _navCooldown > 0f) return;

        Vector2 dir = value.Get<Vector2>();
        if (Mathf.Abs(dir.x) > 0.5f)
            CycleCharacter(dir.x > 0 ? 1 : -1);
    }

    public void OnReady(InputValue value)
    {
        if (!value.isPressed) return;
        ToggleReady();
    }

    public void OnBack(InputValue value)
    {
        if (!value.isPressed) return;

        // Un-ready first; a second Back press could later trigger a leave flow
        if (_isReady)
        {
            _isReady = false;
            _claimedSlot?.SetReady(false);
        }
    }

    // OnSelect is wired in the action asset but intentionally left as a no-op here
    // to prevent accidental UI confirmation bleed through the EventSystem.
    public void OnSelect(InputValue value) { }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void CycleCharacter(int direction)
    {
        _characterIndex = (_characterIndex + direction + Characters.Count) % Characters.Count;
        _navCooldown    = NavCooldownTime;
        _claimedSlot?.SetCharacter(SelectedCharacterId, _characterIndex, Characters.Count);
    }

    private void ToggleReady()
    {
        if (_claimedSlot == null) return;

        _isReady = !_isReady;
        _claimedSlot.SetReady(_isReady);

        if (_isReady)
            _lobbyManager?.OnGhostReady(this);
    }
}