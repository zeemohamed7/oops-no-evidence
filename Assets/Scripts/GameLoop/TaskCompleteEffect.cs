using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the checklist row prefab.
// GameHUD calls Play() automatically via BounceRow; this adds a strikethrough line.
public class TaskCompleteEffect : MonoBehaviour
{
    [Tooltip("Optional strikethrough Image inside the row prefab")]
    public Image strikethroughLine;

    public void Play()
    {
        if (strikethroughLine != null)
        {
            strikethroughLine.gameObject.SetActive(true);
            StartCoroutine(FadeIn(strikethroughLine));
        }
    }

    IEnumerator FadeIn(Image img)
    {
        Color c = img.color;
        c.a = 0f;
        img.color = c;
        float elapsed = 0f, duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            img.color = c;
            yield return null;
        }
    }
}
