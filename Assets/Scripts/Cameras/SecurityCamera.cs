using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    public VisionCone cone; 
    public Light camLight;

    public void Deactivate()
    {
        if (cone != null) cone.enabled = false;
        if (camLight != null) camLight.color = Color.green;
        Debug.Log("Camera Deactivated!");
    }
}