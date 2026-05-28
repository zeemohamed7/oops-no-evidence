using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    // Singleton instance so any script can call SceneTransitionManager.Instance.SwitchToScene()
    public static SceneTransitionManager Instance;

    [Header("Required References")]
    [Tooltip("The Animator component located on your FadeImage child object.")]
    public Animator transitionAnimator;
    
    [Tooltip("The Image component located on your FadeImage child object.")]
    public Image fadeImage; 

    [Header("Transition Settings")]
    [Tooltip("The exact duration of your FadeOut and FadeIn animation clips (in seconds).")]
    public float transitionTime = 1f;

    private void Awake()
    {
        // Singleton pattern: Protect the original canvas across scene loads and destroy duplicates
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Failsafe: Ensure the Animator's GameObject is turned ON so it can process code
        if (transitionAnimator != null)
        {
            transitionAnimator.gameObject.SetActive(true);
        }

        // Failsafe: Turn OFF only the visual Image component so the Lobby isn't hidden by default
        if (fadeImage != null) 
        {
            fadeImage.enabled = false;
        }
    }

    /// <summary>
    /// Call this function from any script to smoothly transition to a new scene.
    /// Example: SceneTransitionManager.Instance.SwitchToScene("Level1");
    /// </summary>
    public void SwitchToScene(string sceneName)
    {
        if (transitionAnimator == null || fadeImage == null)
        {
            Debug.LogError("SceneTransitionManager: Missing inspector assignments! Snapping to scene via backup load.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(LoadSceneSequence(sceneName));
    }

    private IEnumerator LoadSceneSequence(string sceneName)
    {
        // 1. Enable the Image component so the black panel can be seen rendering
        fadeImage.enabled = true;

        // 2. Teleport the animator straight into the "FadeOut" state box instantly
        // (Arguments: "StateName", LayerIndex, NormalizedTime)
        transitionAnimator.Play("FadeOut", 0, 0f);

        // 3. Wait for the screen to turn completely pitch black
        yield return new WaitForSeconds(transitionTime);

        // 4. Load the next scene behind the dark screen veil
        SceneManager.LoadScene(sceneName);

        // 5. Wait a single frame for Unity to properly load and initialize the new scene layers
        yield return null; 

        // 6. Teleport the animator straight into the "FadeIn" state box to reveal the new level
        transitionAnimator.Play("FadeIn", 0, 0f);

        // 7. Wait for the screen to blend back from black to fully clear
        yield return new WaitForSeconds(transitionTime);

        // 8. Turn the Image component back OFF so it doesn't block player clicks/raycasts in gameplay
        fadeImage.enabled = false;
    }
}