using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoadingCleanBar : MonoBehaviour
{
    public Image cleanFill;
    public TMP_Text loadingText;
    public RectTransform mop;

    public float loadingTime = 4f;
    public string nextSceneName = "Lobby";

    private float timer;

    // Warning messages
    string[] earlyWarnings = {
        "Big Boss is watching...",
        "You better not mess this up...",
        "Start cleaning NOW!"
    };

    string[] midWarnings = {
        "You're leaving traces!",
        "This place is still dirty!",
        "Move faster!"
    };

    string[] lateWarnings = {
        "Almost done...",
        "Don't leave anything behind!",
        "Finish it clean!"
    };

    private int currentStage = -1; // to prevent changing every frame

    void Update()
    {
        timer += Time.deltaTime;

        float progress = timer / loadingTime;
        progress = Mathf.Clamp01(progress);

        // Fill animation
        cleanFill.fillAmount = progress;

        // Move mop with progress
        float barWidth = cleanFill.rectTransform.rect.width;
        float startX = -barWidth / 2;
        float endX = barWidth / 2;

        float xPos = Mathf.Lerp(startX, endX, progress);
        mop.anchoredPosition = new Vector2(xPos, mop.anchoredPosition.y);

        // Determine stage (0,1,2)
        int stage;

        if (progress < 0.33f)
            stage = 0;
        else if (progress < 0.66f)
            stage = 1;
        else
            stage = 2;

        // Only change text when stage changes
        if (stage != currentStage)
        {
            currentStage = stage;

            if (stage == 0)
            {
                loadingText.text = earlyWarnings[Random.Range(0, earlyWarnings.Length)];
            }
            else if (stage == 1)
            {
                loadingText.text = midWarnings[Random.Range(0, midWarnings.Length)];
            }
            else
            {
                loadingText.text = lateWarnings[Random.Range(0, lateWarnings.Length)];
            }
        }

        // Load next scene
        if (progress >= 1f)
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}