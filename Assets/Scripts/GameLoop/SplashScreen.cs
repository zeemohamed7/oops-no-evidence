using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────────────────────────────────────
// SplashScreen  —  entry point of the game
// ─────────────────────────────────────────────────────────────────────────────
// Attach to a Canvas GameObject in the Splash scene.
// The splash fades in, waits, then auto-loads the Van Lobby.
// Any key press or click skips the wait immediately.
//
// ── Inspector wiring ──────────────────────────────────────────────────────────
// • splashCanvasGroup  : CanvasGroup on the splash image/logo panel
//                        (set Alpha to 0 in editor so the fade-in works)
// • lobbySceneName     : name of the Van Waiting Lobby scene in Build Settings
// • holdSeconds        : how long to stay fully visible before auto-advancing
// ─────────────────────────────────────────────────────────────────────────────
public class SplashScreen : MonoBehaviour
{
    [Header("Splash Canvas Group")]
    [Tooltip("CanvasGroup on the logo/title panel. Start its Alpha at 0 in the editor.")]
    public CanvasGroup splashCanvasGroup;

    [Header("Timing")]
    public float fadeInSeconds  = 1.5f;
    public float holdSeconds    = 2.5f;
    public float fadeOutSeconds = 1.0f;

    [Header("Scene")]
    public string lobbySceneName = "VanLobby";

    private bool skipped = false;

    private void Start()
    {
        if (splashCanvasGroup != null)
            splashCanvasGroup.alpha = 0f;

        StartCoroutine(SplashRoutine());
    }

    private void Update()
    {
        // Any key press or mouse click skips to the lobby immediately.
        bool anyKey   = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyClick = Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (!skipped && (anyKey || anyClick))
        {
            skipped = true;
            StopAllCoroutines();
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private IEnumerator SplashRoutine()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeInSeconds));

        // Hold
        float elapsed = 0f;
        while (elapsed < holdSeconds && !skipped)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutSeconds));

        if (!skipped)
            SceneManager.LoadScene(lobbySceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (splashCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            splashCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        splashCanvasGroup.alpha = to;
    }
}
