using UnityEngine;

[CreateAssetMenu(fileName = "MissionConditions", menuName = "Mission/Mission Conditions")]
public class MissionConditions : ScriptableObject
{
    [Header("Core Checks")]
    public bool checkTasks = true;
    public bool checkTimer = true;
    public bool checkSuspicion = true;
    public bool checkBlood = true;
    public bool checkFootprints = true;
    public bool checkExtraction = true;

    [Header("Optional Level-Specific Checks")]
    [Tooltip("Only enable on levels that use the furniture-moving mechanic.")]
    public bool checkFurnitureOrganization = false;

    [Header("Blood Detection")]
    [Tooltip("Minimum red-channel value in the blood RenderTexture to count as remaining blood (0–1).")]
    [Range(0.01f, 0.5f)]
    public float bloodDetectionThreshold = 0.05f;

    [Tooltip("Resolution of the downsampled RT scan. Higher = more precise but slower at mission end.")]
    [Range(4, 32)]
    public int bloodScanResolution = 8;

    [Header("Furniture Organization Tolerances")]
    [Tooltip("Max distance (world units) furniture can be from its original position and still pass.")]
    [Range(0.05f, 2f)]
    public float furniturePositionTolerance = 0.3f;

    [Tooltip("Max angle (degrees) furniture can deviate from its original rotation and still pass.")]
    [Range(1f, 45f)]
    public float furnitureRotationTolerance = 5f;

    [Header("Suspicion Override")]
    [Tooltip("Suspicion value that triggers a fail check at extraction. Set to 0 to use SuspicionMeter.maxSuspicion.")]
    [Range(0f, 100f)]
    public float suspicionFailThreshold = 0f;
}
