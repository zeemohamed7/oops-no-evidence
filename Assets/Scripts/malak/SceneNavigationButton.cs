using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneNavigationButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    [Header("Special Scene Options")]
    [SerializeField] private bool hideLobbyUIBeforeLoading = false;
    [SerializeField] private bool destroyLobbyManagerBeforeLoading = false;

    public void LoadTargetScene()
    {
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (hideLobbyUIBeforeLoading)
        {
            HideLobbyUI();
        }

        if (destroyLobbyManagerBeforeLoading)
        {
            DestroyLobbyManagerSafely();

            // Wait one frame so Unity finishes destroying the old LobbyManager
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void HideLobbyUI()
    {
        if (LobbyManager.Instance == null) return;

        GameObject lobbyRoot = LobbyManager.Instance.transform.root.gameObject;

        Canvas[] canvases = lobbyRoot.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = false;
        }

        Debug.Log("[SceneNavigationButton] Lobby UI hidden before loading tutorial.");
    }

    private void DestroyLobbyManagerSafely()
    {
        if (LobbyManager.Instance == null) return;

        GameObject lobbyRoot = LobbyManager.Instance.transform.root.gameObject;

        // Important: clear the static Instance before loading Lobby again
        LobbyManager.Instance = null;

        Destroy(lobbyRoot);

        Debug.Log("[SceneNavigationButton] Old LobbyManager destroyed before returning to Lobby.");
    }
}
