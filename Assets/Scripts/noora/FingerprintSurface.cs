using UnityEngine;

// Add this component to every fingerprint GameObject in the scene.
// FlashlightReveal finds all instances and controls their visibility.
[RequireComponent(typeof(Renderer))]
public class FingerprintSurface : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Renderer>().enabled = false;
    }
}
