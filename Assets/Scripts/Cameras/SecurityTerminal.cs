using UnityEngine;

public class SecurityTerminal : MonoBehaviour
{
    [Header("Hack Settings")]
    public float hackDuration = 5f;
    private float currentProgress = 0f;
    private bool isHacked = false;

    [Header("Scene References")]
    [Tooltip("Drag all the security cameras in the scene here")]
    public VisionCone[] securityCameras;

    public void AddProgress(float deltaTime)
    {
        if (isHacked) return;

        currentProgress += deltaTime;
        Debug.Log($"[HACKING] Progress: {Mathf.Clamp01(currentProgress / hackDuration) * 100f}%");

        if (currentProgress >= hackDuration)
        {
            ExecuteHack();
        }
    }

    public void ResetProgress()
    {
        if (isHacked) return;
        
        if (currentProgress > 0f)
        {
            Debug.Log("[HACKING] Button released, progress lost.");
            currentProgress = 0f;
        }
    }

    private void ExecuteHack()
    {
        isHacked = true;
        currentProgress = hackDuration;
        Debug.Log("[HACKING COMPLETE] All security cameras disabled permanently!");

        foreach (var cameraCone in securityCameras)
        {
            if (cameraCone != null)
            {
                // Disables the VisionCone script so it stops tracking and running FOVRoutine
                cameraCone.enabled = false; 
                
                // Optional visual verification if your camera prefab has a spotlight
                Light camLight = cameraCone.GetComponentInChildren<Light>();
                if (camLight != null) camLight.color = Color.green;
            }
        }
    }
}