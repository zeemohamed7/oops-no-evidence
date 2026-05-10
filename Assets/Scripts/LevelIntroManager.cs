using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelIntroManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI storyText;
    public Button skipButton;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Scene")]
    public string nextSceneName = "Level1_Inst";

    [Header("Text")]
    [TextArea(2, 4)]
    public string[] lines;

    public float typingSpeed = 0.04f;
    public float pauseBetweenLines = 1.2f;

    private bool isSkipping = false;

    void Start()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(LoadNextScene);

        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            LoadNextScene();
    }

    IEnumerator PlayIntro()
    {
        if (storyText != null)
            storyText.text = "";

        if (audioSource != null)
            audioSource.Play();

        if (storyText != null && lines != null)
        {
            foreach (string line in lines)
            {
                yield return StartCoroutine(TypeLine(line));
                yield return new WaitForSeconds(pauseBetweenLines);
            }
        }

        yield return new WaitForSeconds(1f);
        LoadNextScene();
    }

    IEnumerator TypeLine(string line)
    {
        storyText.text = "";

        foreach (char letter in line)
        {
            storyText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    void LoadNextScene()
    {
        if (isSkipping) return;

        isSkipping = true;
        SceneManager.LoadScene(nextSceneName);
    }
}