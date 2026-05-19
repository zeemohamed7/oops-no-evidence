using System.Collections;
using UnityEngine;

/// <summary>
/// Add one of these to each level scene. Enable only the tasks that appear
/// in that level's HUD task list. It polls gameplay state and fires
/// GameEvents.OnTaskCompleted so the HUD checkmarks update in real-time.
///
/// Level 1: trackBlood only
/// Level 2: trackBlood + trackFurniture
/// Level 3: trackBlood + trackFurniture + trackFingerprints
/// Level 4: trackBlood + trackFurniture + trackFingerprints (cameras handled separately)
/// </summary>
public class TaskCompletionTracker : MonoBehaviour
{
    [Header("Tasks to track for this level")]
    public bool trackBlood = false;
    public bool trackFurniture = false;
    public bool trackFingerprints = false;

    [Header("Blood Scan (GPU readback — keep interval >= 2s)")]
    public float bloodPollInterval = 2f;
    [Tooltip("Match MissionConditions.bloodDetectionThreshold.")]
    [Range(0.01f, 0.5f)] public float bloodThreshold = 0.05f;
    [Tooltip("Match MissionConditions.bloodScanResolution.")]
    [Range(4, 32)] public int bloodScanResolution = 8;

    [Header("Furniture (must match MissionResultManager furnitureGroups)")]
    public FurnitureOrganizationChecker[] furnitureGroups;
    [Range(0.05f, 2f)] public float furniturePosTolerance = 0.3f;
    [Range(1f, 45f)]  public float furnitureRotTolerance = 5f;

    bool _bloodDone;
    bool _furnitureDone;
    bool _fingerprintsDone;

    FingerprintSurface[] _fingerprints;

    void Start()
    {
        if (trackFingerprints)
            _fingerprints = FindObjectsByType<FingerprintSurface>(FindObjectsSortMode.None);

        if (trackBlood)
            StartCoroutine(PollBlood());
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (trackFurniture    && !_furnitureDone)    CheckFurnitureDone();
        if (trackFingerprints && !_fingerprintsDone) CheckFingerprintsDone();
    }

    IEnumerator PollBlood()
    {
        var wait = new WaitForSeconds(bloodPollInterval);
        while (!_bloodDone)
        {
            yield return wait;
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) continue;

            bool anyBlood = false;
            foreach (var pool in FindObjectsByType<BloodPool>(FindObjectsSortMode.None))
            {
                if (pool.HasBloodRemaining(bloodThreshold, bloodScanResolution))
                {
                    anyBlood = true;
                    break;
                }
            }

            if (!anyBlood)
            {
                _bloodDone = true;
                GameEvents.OnTaskCompleted?.Invoke("clean_blood");
            }
        }
    }

    void CheckFurnitureDone()
    {
        if (furnitureGroups == null || furnitureGroups.Length == 0) return;

        foreach (var group in furnitureGroups)
        {
            if (group == null) continue;
            if (!group.IsOrganized(furniturePosTolerance, furnitureRotTolerance)) return;
        }

        _furnitureDone = true;
        GameEvents.OnTaskCompleted?.Invoke("rearrange_furniture");
    }

    void CheckFingerprintsDone()
    {
        if (_fingerprints == null || _fingerprints.Length == 0) return;

        foreach (var fp in _fingerprints)
            if (fp != null && !fp.IsCleaned) return;

        _fingerprintsDone = true;
        GameEvents.OnTaskCompleted?.Invoke("clean_fingerprints");
    }
}
