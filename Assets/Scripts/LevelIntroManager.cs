using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelIntroManager : MonoBehaviour
{
    public TextMeshProUGUI storyText;
    public AudioSource audioSource;

    public string nextSceneName = "Level1_Gameplay";

    [TextArea(2, 4)]
    public string[] lines;

    public float typingSpeed = 0.04f;
    public float pauseBetweenLines = 1.2f;

    private bool isSkipping = false;

    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextScene();
        }
    }

    IEnumerator PlayIntro()
    {
        storyText.text = "";

        if (audioSource != null)
        {
            audioSource.Play();
        }

        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(pauseBetweenLines);
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

    public void LoadNextScene()
    {
        if (isSkipping) return;

        isSkipping = true;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.SwitchToScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}