using UnityEngine;
using UnityEngine.Events;

public class SuspicionMeter : MonoBehaviour
{
    public static SuspicionMeter Instance;

    [Header("Global Meter")] public float globalSuspicion;

    public float maxSuspicion = 100f;

    public UnityEvent OnGameOver;

    private void Awake()
    {
        // Setup the Singleton so any guard can find this instantly
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ModifySuspicion(float amount)
    {
        globalSuspicion += amount;
        globalSuspicion = Mathf.Clamp(globalSuspicion, 0, maxSuspicion);

        if (globalSuspicion >= maxSuspicion)
        {
            Debug.Log("GAME OVER: Max Suspicion Reached!");
            OnGameOver.Invoke();
        }
    }
}