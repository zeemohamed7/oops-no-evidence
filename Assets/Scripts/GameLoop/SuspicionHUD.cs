using UnityEngine;
using UnityEngine.UI;

public class SuspicionHUD : MonoBehaviour
{
    [Header("UI")]
    public Image fillBar;

    private void Update()
    {
        if (SuspicionMeter.Instance == null || fillBar == null)
            return;

        float percent = SuspicionMeter.Instance.globalSuspicion / SuspicionMeter.Instance.maxSuspicion;
        fillBar.fillAmount = Mathf.Clamp01(percent);
    }
}