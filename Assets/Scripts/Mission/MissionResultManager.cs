using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class MissionResultManager : MonoBehaviour
{
    public static MissionResultManager Instance;

    [Header("Configuration")]
    [Tooltip("ScriptableObject that defines which checks are enabled for this level.")]
    public MissionConditions conditions;

    [Header("Scene References")]
    [Tooltip("The van/extraction trigger zone.")]
    public ExtractionZone extractionZone;

    [Tooltip("Furniture groups to check — only used when conditions.checkFurnitureOrganization is true.")]
    public List<FurnitureOrganizationChecker> furnitureGroups = new List<FurnitureOrganizationChecker>();

    [Header("Events")]
    [Tooltip("Fired when the mission is failed. Passes the list of failure reason strings.")]
    public UnityEvent<List<string>> OnMissionFailed;

    [Tooltip("Fired when all conditions pass.")]
    public UnityEvent OnMissionPassed;

    private bool _evaluated = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (extractionZone != null)
            extractionZone.OnAllPlayersExtracted.AddListener(EvaluateMission);
    }

    void OnDisable()
    {
        if (extractionZone != null)
            extractionZone.OnAllPlayersExtracted.RemoveListener(EvaluateMission);
    }
    public void EvaluateMission()
    {
        if (_evaluated) return;
        _evaluated = true;

        Debug.Log("[MissionResultManager] *** EvaluateMission called ***");

        if (conditions == null)
        {
            Debug.LogWarning("[MissionResultManager] No MissionConditions assigned — defaulting to Win.");
            FireWin();
            return;
        }

        Debug.Log($"[MissionResultManager] Conditions: checkTasks={conditions.checkTasks}, checkTimer={conditions.checkTimer}, " +
                  $"checkSuspicion={conditions.checkSuspicion}, checkBlood={conditions.checkBlood}, " +
                  $"checkFootprints={conditions.checkFootprints}, checkFurnitureOrganization={conditions.checkFurnitureOrganization}, " +
                  $"checkWallFingerprints={conditions.checkWallFingerprints}");
        Debug.Log($"[MissionResultManager] furnitureGroups assigned in Inspector: {furnitureGroups.Count}");

        List<string> failures = CollectFailures();

        if (failures.Count == 0)
            FireWin();
        else
            FireLoss(failures);
    }

    public void ForceTimerLoss()
    {
        ForceLoss("You failed to escape in time");
    }

    public void ForceSuspicionLoss()
    {
        ForceLoss("Suspicion became too high");
    }

    void ForceLoss(string reason)
    {
        if (_evaluated) return;
        _evaluated = true;
        FireLoss(new List<string> { reason });
    }

    List<string> CollectFailures()
    {
        var failures = new List<string>();

        if (conditions.checkTasks && !CheckTasks())
            failures.Add("Not all tasks were completed");

        if (conditions.checkTimer && !CheckTimer())
            failures.Add("You failed to escape in time");

        if (conditions.checkSuspicion && !CheckSuspicion())
            failures.Add("Suspicion became too high");

        if (conditions.checkBlood && !CheckBlood())
            failures.Add("Blood evidence was left behind");

        if (conditions.checkFootprints && !CheckFootprints())
            failures.Add("Footprints were detected");

        if (conditions.checkFurnitureOrganization && !CheckFurniture())
            failures.Add("Furniture was not restored");

        if (conditions.checkWallFingerprints && !CheckWallFingerprints())
            failures.Add("Fingerprints were not cleaned");

        return failures;
    }

    bool CheckTasks()
    {
        // Passes if HUD doesn't exist (no tasks) or all tasks are ticked
        return GameHUD.Instance == null || GameHUD.Instance.AllTasksDone();
    }

    bool CheckTimer()
    {
        return GameManager.Instance == null || GameManager.Instance.TimeRemaining > 0f;
    }

    bool CheckSuspicion()
    {
        if (SuspicionMeter.Instance == null) return true;
        float threshold = conditions.suspicionFailThreshold > 0f
            ? conditions.suspicionFailThreshold
            : SuspicionMeter.Instance.maxSuspicion;
        return SuspicionMeter.Instance.globalSuspicion < threshold;
    }

    bool CheckBlood()
    {
        BloodPool[] pools = FindObjectsByType<BloodPool>(FindObjectsSortMode.None);
        foreach (BloodPool pool in pools)
        {
            if (pool.HasBloodRemaining(conditions.bloodDetectionThreshold, conditions.bloodScanResolution))
                return false;
        }
        return true;
    }

    bool CheckFootprints()
    {
        FootprintTracker.ActiveFootprints.RemoveAll(fp => fp == null);
        return FootprintTracker.ActiveFootprints.Count == 0;
    }

    bool CheckFurniture()
    {
        if (furnitureGroups.Count == 0)
        {
            Debug.LogWarning("[MissionResultManager] CheckFurniture — furnitureGroups list is EMPTY! " +
                             "Assign at least one FurnitureOrganizationChecker to MissionResultManager in the Inspector. " +
                             "Returning true (pass) vacuously — furniture condition will never fail.");
            return true;
        }

        bool overallResult = true;
        foreach (FurnitureOrganizationChecker group in furnitureGroups)
        {
            if (group == null) continue;
            bool organized = group.IsOrganized(conditions.furniturePositionTolerance, conditions.furnitureRotationTolerance, verbose: true);
            Debug.Log($"[MissionResultManager] FurnitureGroup '{group.name}': IsOrganized = {(organized ? "PASS ✓" : "FAIL ✗")}");
            if (!organized) overallResult = false;
        }
        Debug.Log($"[MissionResultManager] CheckFurniture overall result: {(overallResult ? "PASS ✓" : "FAIL ✗")}");
        return overallResult;
    }

    bool CheckWallFingerprints()
    {
        var fingerprints = FindObjectsByType<FingerprintSurface>(FindObjectsSortMode.None);
        foreach (var fp in fingerprints)
            if (!fp.IsCleaned) return false;
        return true;
    }

    void FireWin()
    {
        OnMissionPassed.Invoke();
        GameManager.Instance?.TriggerWin();
        Debug.Log("[MissionResultManager] MISSION PASSED");
    }

    void FireLoss(List<string> reasons)
    {
        OnMissionFailed.Invoke(reasons);
        GameManager.Instance?.TriggerLoss(reasons);
        Debug.Log($"[MissionResultManager] MISSION FAILED — {string.Join(" | ", reasons)}");
    }
}
