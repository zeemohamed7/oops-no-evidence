using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskCompletionTracker : MonoBehaviour
{
    [Header("Tasks to track for this level")]
    public bool trackBlood = false;
    public bool trackFingerprints = false;
    public bool trackCameras = false;
    public bool trackFurniture = false;

    [Header("Blood Scan (GPU readback — keep interval >= 2s)")]
    public float bloodPollInterval = 2f;
    [Tooltip("Match MissionConditions.bloodDetectionThreshold.")]
    [Range(0.01f, 0.5f)] public float bloodThreshold = 0.05f;
    [Tooltip("Match MissionConditions.bloodScanResolution.")]
    [Range(4, 32)] public int bloodScanResolution = 8;

    [Header("Furniture (only used when trackFurniture = true)")]
    [Tooltip("Drag the same FurnitureOrganizationChecker(s) you assigned to MissionResultManager.")]
    public List<FurnitureOrganizationChecker> furnitureGroups = new List<FurnitureOrganizationChecker>();
    [Tooltip("Must match MissionConditions.furniturePositionTolerance.")]
    [Range(0.05f, 2f)] public float furniturePosTolerance = 0.3f;
    [Tooltip("Must match MissionConditions.furnitureRotationTolerance.")]
    [Range(1f, 45f)] public float furnitureRotTolerance = 5f;

    bool _bloodDone;
    bool _fingerprintsDone;
    bool _camerasDone;
    bool _furnitureDone;

    FingerprintSurface[]  _fingerprints;
    SecurityTerminal[]    _terminals;

    void Start()
    {
        if (trackFingerprints)
            _fingerprints = FindObjectsByType<FingerprintSurface>(FindObjectsSortMode.None);

        if (trackCameras)
            _terminals = FindObjectsByType<SecurityTerminal>(FindObjectsSortMode.None);

        if (trackBlood)
            StartCoroutine(PollBlood());

        if (trackFurniture && furnitureGroups.Count == 0)
            Debug.LogWarning("[TaskCompletionTracker] trackFurniture is true but furnitureGroups list is EMPTY. " +
                             "Drag FurnitureOrganizationChecker scene object(s) into the Furniture Groups list.");
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        if (trackFingerprints && !_fingerprintsDone) CheckFingerprintsDone();
        if (trackCameras      && !_camerasDone)      CheckCamerasDone();
        if (trackFurniture    && !_furnitureDone)     CheckFurnitureDone();
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

    void CheckFingerprintsDone()
    {
        if (_fingerprints == null || _fingerprints.Length == 0) return;

        foreach (var fp in _fingerprints)
            if (fp != null && !fp.IsCleaned) return;

        _fingerprintsDone = true;
        GameEvents.OnTaskCompleted?.Invoke("clean_fingerprints");
    }

    void CheckCamerasDone()
    {
        if (_terminals == null || _terminals.Length == 0) return;

        foreach (var t in _terminals)
            if (t != null && !t.IsComplete) return;

        _camerasDone = true;
        GameEvents.OnTaskCompleted?.Invoke("disable_cameras");
    }

    void CheckFurnitureDone()
    {
        if (furnitureGroups.Count == 0) return;

        foreach (FurnitureOrganizationChecker group in furnitureGroups)
        {
            if (group == null) continue;
            if (!group.IsOrganized(furniturePosTolerance, furnitureRotTolerance))
                return;
        }

        _furnitureDone = true;
        Debug.Log("[TaskCompletionTracker] All furniture organized → firing 'rearrange_furniture'");
        GameEvents.OnTaskCompleted?.Invoke("rearrange_furniture");
    }
}