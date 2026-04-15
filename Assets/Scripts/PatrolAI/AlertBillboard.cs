using UnityEngine;

public class AlertBillboard : MonoBehaviour
{
    private Transform cam;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        // Make the UI face the camera every frame
        transform.LookAt(transform.position + cam.forward);
    }
}