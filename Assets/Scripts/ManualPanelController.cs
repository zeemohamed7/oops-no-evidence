using UnityEngine;
using UnityEngine.UI;

public class ManualPanelController : MonoBehaviour
{
    [Header("Sub Panels")]
    public GameObject panelKeyboard;
    public GameObject panelGamepad;

    [Header("Tab Buttons (Optional Polish)")]
    public Button buttonKeyboard;
    public Button buttonGamepad;

    void Start()
    {
        // Default to showing the keyboard layout when the panel first opens
        ShowKeyboardPanel();
    }

    public void ShowKeyboardPanel()
    {
        panelKeyboard.SetActive(true);
        panelGamepad.SetActive(false);

        // Optional: Make the active tab look highlighted
        SetButtonAlpha(buttonKeyboard, 1.0f);
        SetButtonAlpha(buttonGamepad, 0.5f);
    }

    public void ShowGamepadPanel()
    {
        panelKeyboard.SetActive(false);
        panelGamepad.SetActive(true);

        // Optional: Make the active tab look highlighted
        SetButtonAlpha(buttonKeyboard, 0.5f);
        SetButtonAlpha(buttonGamepad, 1.0f);
    }

    private void SetButtonAlpha(Button button, float alpha)
    {
        if (button != null)
        {
            Color c = button.image.color;
            c.a = alpha;
            button.image.color = c;
        }
    }
}