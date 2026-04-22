using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelector : MonoBehaviour
{
    public int sceneIndex;

    private Renderer rend;
    private Color originalColor;
    private Vector3 originalScale;

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalScale = transform.localScale;

        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    void OnMouseEnter()
    {
        if (rend != null)
        {
            rend.material.color = originalColor * 1.3f;
        }

        transform.localScale = originalScale * 1.1f;
    }

    void OnMouseExit()
    {
        if (rend != null)
        {
            rend.material.color = originalColor;
        }

        transform.localScale = originalScale;
    }

    void OnMouseDown()
    {
        if (rend != null)
        {
            rend.material.color = originalColor * 0.9f;
        }

        Invoke(nameof(LoadLevel), 0.15f);
    }

    void LoadLevel()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}