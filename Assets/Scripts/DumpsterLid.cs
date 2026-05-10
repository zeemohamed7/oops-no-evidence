using System.Collections;
using UnityEngine;

public class DumpsterLid : MonoBehaviour
{
    [Header("Lid Transform")]
    [Tooltip("Leave empty to auto-create a simple flat box at runtime")]
    public Transform lid;

    [Header("Auto-Create Lid  (ignored when Lid is assigned)")]
    [Tooltip("World-space width of the generated lid")]
    public float lidWorldWidth  = 4f;
    [Tooltip("World-space depth of the generated lid")]
    public float lidWorldDepth  = 2f;
    [Tooltip("World-space thickness")]
    public float lidWorldThick  = 0.08f;
    [Tooltip("World-space height above this object's position")]
    public float lidWorldHeight = 1.4f;

    [Header("Open Angle")]
    [Tooltip("Degrees the lid swings open around its local X axis")]
    public float openAngle = 110f;

    [Header("Detection")]
    [Tooltip("World-space radius to sense a nearby Weapon")]
    public float detectionRadius = 2.5f;

    [Header("Timing")]
    public float openSpeed  = 0.35f;
    public float closeDelay = 1.2f;
    public float closeSpeed = 0.5f;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip   openClip;
    public AudioClip   closeClip;

    private Quaternion _closedRot;
    private Quaternion _openRot;
    private bool       _isOpen;
    private bool       _weaponNearby;
    private Coroutine  _animCoroutine;
    private Coroutine  _closeDelayCoroutine;

    void Start()
    {
        if (lid == null)
            lid = BuildDefaultLid();

        _closedRot = lid.localRotation;
        _openRot   = lid.localRotation * Quaternion.Euler(-openAngle, 0f, 0f);

        Debug.Log($"[DumpsterLid] Start OK — position: {transform.position}, radius: {detectionRadius}");
        StartCoroutine(DetectionLoop());
    }

    // ── Weapon detection (OverlapSphere every 0.1 s — no trigger collider needed) ──

    private IEnumerator DetectionLoop()
    {
        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            bool found = false;
            Collider[] hits = Physics.OverlapSphere(
                transform.position, detectionRadius, ~0, QueryTriggerInteraction.Ignore);

            Debug.Log($"[DumpsterLid] OverlapSphere found {hits.Length} colliders near {transform.position}");
            foreach (var col in hits)
            {
                Debug.Log($"[DumpsterLid]   - {col.name}  tag={col.tag}");
                if (col.CompareTag("Weapon")) { found = true; break; }
            }

            if (found && !_weaponNearby)
            {
                _weaponNearby = true;
                if (_closeDelayCoroutine != null)
                {
                    StopCoroutine(_closeDelayCoroutine);
                    _closeDelayCoroutine = null;
                }
                SetLid(open: true);
            }
            else if (!found && _weaponNearby)
            {
                _weaponNearby = false;
                _closeDelayCoroutine = StartCoroutine(DelayedClose());
            }

            yield return wait;
        }
    }

    // ── Lid animation ──

    private void SetLid(bool open)
    {
        if (lid == null || _isOpen == open) return;
        _isOpen = open;

        if (_animCoroutine != null) StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimateLid(
            open ? _openRot : _closedRot,
            open ? openSpeed : closeSpeed));

        if (audioSource != null)
        {
            AudioClip clip = open ? openClip : closeClip;
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator DelayedClose()
    {
        yield return new WaitForSeconds(closeDelay);
        SetLid(open: false);
        _closeDelayCoroutine = null;
    }

    private IEnumerator AnimateLid(Quaternion target, float duration)
    {
        Quaternion start = lid.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            lid.localRotation = Quaternion.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        lid.localRotation = target;
        _animCoroutine = null;
    }

    // ── Default lid builder (world-space aware) ──

    private Transform BuildDefaultLid()
    {
        // Hinge sits at the back-top edge of the dumpster in world space
        Vector3 worldHingePos = transform.position
            + Vector3.up   * lidWorldHeight
            - transform.forward * lidWorldDepth * 0.5f;

        GameObject hinge = new GameObject("DumpsterLid_Hinge");
        hinge.transform.position = worldHingePos;
        hinge.transform.rotation = transform.rotation;
        hinge.transform.SetParent(transform, worldPositionStays: true);

        // Mesh child — use lossyScale to cancel out parent scale
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "DumpsterLid_Mesh";
        mesh.transform.SetParent(hinge.transform, worldPositionStays: false);

        Vector3 ls = hinge.transform.lossyScale;
        mesh.transform.localPosition = new Vector3(0f, 0f,  lidWorldDepth * 0.5f / ls.z);
        mesh.transform.localScale    = new Vector3(
            lidWorldWidth / ls.x,
            lidWorldThick / ls.y,
            lidWorldDepth / ls.z);

        Destroy(mesh.GetComponent<Collider>());
        return hinge.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
