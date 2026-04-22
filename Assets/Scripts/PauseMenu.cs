using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseCanvas;

    public Image soundIcon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private bool isPaused = false;
    private bool isSoundOn = true;

    void Start()
    {
        pauseCanvas.SetActive(false);

        // Load saved sound state
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

        ApplySound();
        UpdateSoundUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseCanvas.SetActive(true);
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

    private void ApplySound()
    {
        AudioListener.volume = isSoundOn ? 1f : 0f;
    }

    private void UpdateSoundUI()
    {
        if (soundIcon != null)
        {
            soundIcon.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
        }
    }

        public void ReplayLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}