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
        // 🟢 FIXED: Forcibly unfreeze time tracking so typing calculations can advance 
        // even if the previous gameplay level left Time.timeScale at 0
        Time.timeScale = 1f;

        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        // 🟢 FIXED: Only check input if a transition isn't actively occurring
        if (!isSkipping && Input.GetKeyDown(KeyCode.Space))
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
            // If the player hit skip, immediately halt processing this sequence
            if (isSkipping) yield break;

            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(pauseBetweenLines);
        }

        if (!isSkipping)
        {
            yield return new WaitForSeconds(1f);
            LoadNextScene();
        }
    }

    IEnumerator TypeLine(string line)
    {
        storyText.text = "";

        foreach (char letter in line)
        {
            if (isSkipping) yield break;

            storyText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void LoadNextScene()
    {
        if (isSkipping) return;
        isSkipping = true;

        // Stop the background audio immediately so it doesn't leak into the next layout load frame
        if (audioSource != null)
        {
            audioSource.Stop();
        }

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