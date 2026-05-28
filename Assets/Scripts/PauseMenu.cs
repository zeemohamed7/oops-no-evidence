using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; // 🟢 Required

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseCanvas;

    [Header("New Input Setup")]
    [Tooltip("Drag the GameObject that has your PlayerInput component here")]
    public PlayerInput playerInput; 
    public string pauseActionName = "Pause"; // Matches the name in your Input Actions window

    public Image soundIcon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private bool isPaused = false;
    private bool isSoundOn = true;

    void Start()
    {
        pauseCanvas.SetActive(false);
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        ApplySound();
        UpdateSoundUI();
    }

    void Update()
    {
        // ─── CHECK INPUT VIA INPUT ACTIONS ENGINE ───
        bool pressedPause = false;

        if (playerInput != null)
        {
            // This checks whatever keys you bound to "Pause" in your asset window!
            if (playerInput.actions[pauseActionName].wasPressedThisFrame)
            {
                pressedPause = true;
            }
        }
        else
        {
            // Fallback to your easy method just in case you forgot to drag the reference in the inspector
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
            {
                pressedPause = true;
            }
        }

        // ─── EXECUTE PAUSE ───
        if (pressedPause)
        {
            if (WinLoseScreenManager.Instance != null &&
                WinLoseScreenManager.Instance.panelRoot != null &&
                WinLoseScreenManager.Instance.panelRoot.activeSelf) return;

            if (isPaused) ResumeGame();
            else          PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseCanvas.SetActive(true);
        var canvas = pauseCanvas.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 50;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySound();
        UpdateSoundUI();
    }

    private void ApplySound() => AudioListener.volume = isSoundOn ? 1f : 0f;
    private void UpdateSoundUI() { if (soundIcon != null) soundIcon.sprite = isSoundOn ? soundOnSprite : soundOffSprite; }
    public void ReplayLevel() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("Lobby"); }
}