using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NeighborVisitor : MonoBehaviour
{
    public enum State { Waiting, Entering, Scanning, Leaving }

    [Header("Path Waypoints (in order: start → ... → scan spot)")]
    [Tooltip("Place waypoints from neighbor villa through each door to the inside scan position.")]
    public Transform[] waypoints;

    [Header("Doors")]
    [Tooltip("All HingeDoors to open when passing through.")]
    public HingeDoor[] doors;
    [Tooltip("Which waypoint index is at the door frame.")]
    public int doorWaypointIndex = 1;
    [Tooltip("How close the NPC must be to the door waypoint before doors open.")]
    public float doorOpenRadius = 2.5f;
    [Tooltip("Seconds doors stay open after NPC passes.")]
    public float doorOpenDuration = 3f;

    [Header("Visit Timing")]
    public float waitBetweenVisits = 25f;
    public float scanDuration      = 5f;
    public float scanTurnSpeed     = 1.2f;
    public float scanAngle         = 75f;

    [Header("Suspicion")]
    public float susPerSecondSeen  = 20f;
    public float susOnBloodSpotted = 35f;

    [Header("Audio")]
    public AudioClip knockSound;
    public AudioClip reactionSound;

    // ── Private ───────────────────────────────────────────────────────────
    NavMeshAgent  _agent;
    Animator      _anim;
    VisionCone    _vision;
    AudioSource   _audio;
    BloodPool[]   _bloodPools;

    State   _state     = State.Waiting;
    float   _waitTimer;
    float   _scanTimer;
    bool    _bloodChecked;
    int     _waypointIndex;
    bool    _goingIn;
    Quaternion _scanStartRot;
    float   _pathGraceTimer;
    const float PathGrace = 1.5f;
    bool      _doorOpened;
    Coroutine _closeDoorsCoroutine;

    void Start()
    {
        _agent     = GetComponent<NavMeshAgent>();
        _anim      = GetComponentInChildren<Animator>();
        _vision    = GetComponent<VisionCone>();
        _audio     = GetComponent<AudioSource>();
        _bloodPools = FindObjectsByType<BloodPool>(FindObjectsSortMode.None);

        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            transform.position = waypoints[0].position;

        _waitTimer = waitBetweenVisits;
        _agent.isStopped = true;
        SetAnim(false);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        // // Open doors only when entering — leaving uses the same baked NavMesh gap, no visual reopen needed
        // if (_state == State.Entering && !_doorOpened
        //     && waypoints != null && doorWaypointIndex < waypoints.Length
        //     && waypoints[doorWaypointIndex] != null)
        // {
        //     float dist = Vector3.Distance(transform.position, waypoints[doorWaypointIndex].position);
        //     if (dist <= doorOpenRadius)
        //     {
        //         _doorOpened = true;
        //         OpenDoors();
        //     }
        // }

        switch (_state)
        {
            case State.Waiting:  UpdateWaiting();  break;
            case State.Entering: UpdateEntering(); break;
            case State.Scanning: UpdateScanning(); break;
            case State.Leaving:  UpdateLeaving();  break;
        }
    }

    // ── States ─────────────────────────────────────────────────────────────

    void UpdateWaiting()
    {
        _waitTimer -= Time.deltaTime;
        if (_waitTimer > 0f) return;

        if (waypoints == null || waypoints.Length < 2) return;

        PlaySound(knockSound);
        _goingIn = true;
        _waypointIndex = 1; // start from index 1 (index 0 is the starting position)
        _bloodChecked = false;
        _doorOpened = false;
        _state = State.Entering;
        _agent.isStopped = false;
        _agent.SetDestination(waypoints[_waypointIndex].position);
        SetAnim(true);
    }

    void UpdateEntering()
    {
        if (_agent.pathPending) { _pathGraceTimer = 0f; return; }

        if (!_agent.hasPath)
        {
            _pathGraceTimer += Time.deltaTime;
            if (_pathGraceTimer < PathGrace) return;
            _pathGraceTimer = 0f;
            // Can't reach next waypoint — abort visit
            ReturnToStart();
            return;
        }

        _pathGraceTimer = 0f;
        if (_agent.remainingDistance > _agent.stoppingDistance + 0.3f) return;

        _waypointIndex++;

        if (_waypointIndex < waypoints.Length)
        {
            // Move to next waypoint
            _agent.SetDestination(waypoints[_waypointIndex].position);
        }
        else
        {
            // Reached the last waypoint — start scanning
            _state = State.Scanning;
            _agent.isStopped = true;
            _scanTimer    = 0f;
            _scanStartRot = transform.rotation;
            SetAnim(false);
            CheckForBlood();
        }
    }

    void UpdateScanning()
    {
        _scanTimer += Time.deltaTime;

        if (_vision != null && _vision.canSeePlayer)
        {
            // Freeze and stare at the player while adding sus — no turning, no walking
            if (_vision.playerRef != null)
            {
                Vector3 dir = (_vision.playerRef.transform.position - transform.position);
                dir.y = 0f;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
            }

            PlaySound(reactionSound);
            GameEvents.OnSuspicionAdded?.Invoke(susPerSecondSeen * Time.deltaTime);
        }
        else
        {
            // Resume scan rotation when player not visible
            float angle = Mathf.Sin(Time.time * scanTurnSpeed) * scanAngle;
            transform.rotation = _scanStartRot * Quaternion.Euler(0f, angle, 0f);
        }

        if (_scanTimer >= scanDuration)
            StartLeaving();
    }

    void UpdateLeaving()
    {
        if (_agent.pathPending) { _pathGraceTimer = 0f; return; }

        if (!_agent.hasPath)
        {
            _pathGraceTimer += Time.deltaTime;
            if (_pathGraceTimer < PathGrace) return;
            _pathGraceTimer = 0f;
            ReturnToStart();
            return;
        }

        _pathGraceTimer = 0f;
        if (_agent.remainingDistance > _agent.stoppingDistance + 0.3f) return;

        ReturnToStart();
    }

    void ReturnToStart()
    {
        _agent.isStopped = true;
        _state     = State.Waiting;
        _waitTimer = waitBetweenVisits;
        if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            _agent.Warp(waypoints[0].position);
        SetAnim(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void StartLeaving()
    {
        _state = State.Leaving;
        _agent.isStopped = false;
        SetAnim(true);
        _agent.SetDestination(waypoints[0].position);
    }

    // void OpenDoors()
    // {
    //     if (doors == null) return;
    //     foreach (var d in doors)
    //         if (d != null) d.OpenFromSide(true);
    //
    //     // Cancel any existing close timer so the door doesn't snap shut mid-open
    //     if (_closeDoorsCoroutine != null)
    //         StopCoroutine(_closeDoorsCoroutine);
    //     _closeDoorsCoroutine = StartCoroutine(CloseDoorsDelayed());
    // }
    //
    // IEnumerator CloseDoorsDelayed()
    // {
    //     yield return new WaitForSeconds(doorOpenDuration);
    //     if (doors == null) yield break;
    //     foreach (var d in doors)
    //         if (d != null) d.CloseDoor();
    //     _closeDoorsCoroutine = null;
    // }

    void CheckForBlood()
    {
        if (_bloodChecked) return;
        _bloodChecked = true;

        Transform scanSpot = waypoints[waypoints.Length - 1];
        foreach (BloodPool pool in _bloodPools)
        {
            if (pool == null) continue;
            if (!pool.IsNearby(scanSpot.position, 8f)) continue;
            if (!pool.HasBloodRemaining(0.05f, 8)) continue;

            PlaySound(reactionSound);
            GameEvents.OnSuspicionAdded?.Invoke(susOnBloodSpotted);
            break;
        }
    }

    void SetAnim(bool walking)
    {
        if (_anim == null) return;
        _anim.SetFloat("Vert",  walking ? 1f : 0f);
        _anim.SetFloat("State", 0f);
    }

    void PlaySound(AudioClip clip)
    {
        if (_audio == null || clip == null) return;
        if (!_audio.isPlaying) _audio.PlayOneShot(clip);
    }
}