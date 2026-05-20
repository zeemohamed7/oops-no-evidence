using UnityEngine;

// Add this component to every fingerprint GameObject in the scene.
// FlashlightReveal controls visibility; WallSprayCleaner calls Clean().
[RequireComponent(typeof(Renderer))]
public class FingerprintSurface : MonoBehaviour
{
    public bool IsCleaned { get; private set; }

    void Awake()
    {
        GetComponent<Renderer>().enabled = false;
    }

    public void Clean()
    {
        IsCleaned = true;
    }
}
