using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject instructionsPanel;
    public GameObject settingsPanel;

    public Image soundIcon;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    public Image musicIcon;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    private bool isSoundOn = true;
    private bool isMusicOn = true;

    private void Start()
    {
        LoadSettings();
        ShowMainMenu();
        UpdateUI();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        instructionsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void ShowInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;

        AudioListener.volume = isSoundOn ? 1f : 0f;

        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateUI();
    }


    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;

        // This only works if you have a music AudioSource later

        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (soundIcon != null)
            soundIcon.sprite = isSoundOn ? soundOnSprite : soundOffSprite;

        if (musicIcon != null)
            musicIcon.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
    }

    private void LoadSettings()
    {
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        AudioListener.volume = isSoundOn ? 1f : 0f;
    }
}