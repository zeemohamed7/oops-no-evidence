using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("Volume")]
    public Slider volumeSlider;

    [Header("Full Screen")]
    public Toggle fullScreenToggle;

    [Header("Resolution")]
    public TMP_Dropdown resolutionDropdown;

    private Resolution[] resolutions;

    private void Start()
    {
        LoadVolume();
        LoadFullScreen();
        BuildResolutionDropdown();

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        fullScreenToggle.onValueChanged.AddListener(OnFullScreenChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    // --- Volume ---

    private void LoadVolume()
    {
        float saved = PlayerPrefs.GetFloat("Volume", 1f);
        volumeSlider.value = saved;
        AudioListener.volume = saved;
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    // --- Full Screen ---

    private void LoadFullScreen()
    {
        bool saved = PlayerPrefs.GetInt("FullScreen", Screen.fullScreen ? 1 : 0) == 1;
        fullScreenToggle.isOn = saved;
        Screen.fullScreen = saved;
    }

    private void OnFullScreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("FullScreen", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // --- Resolution ---

    private void BuildResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int savedIndex = PlayerPrefs.GetInt("ResolutionIndex", -1);
        int currentIndex = 0;

        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = savedIndex >= 0 ? savedIndex : currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void OnResolutionChanged(int index)
    {
        Resolution r = resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }
}