using UnityEngine;
using UnityEngine.InputSystem;
public class GrabTester : MonoBehaviour
{
    public GrabbableObject targetObject;

    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            targetObject.TryGrab(gameObject);
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            targetObject.Release();
        }
    }
}
