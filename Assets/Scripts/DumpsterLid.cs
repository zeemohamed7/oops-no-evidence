using System.Collections;
using UnityEngine;

public class DumpsterLid : MonoBehaviour
{
    [Header("Lid Transform")]
    [Tooltip("Leave empty to auto-create a simple box lid at runtime")]
    public Transform lid;

    [Header("Auto-Create Lid (used when Lid is empty)")]
    [Tooltip("Width of the auto-created lid (match dumpster opening)")]
    public float lidWidth  = 1f;
    [Tooltip("Depth of the auto-created lid (match dumpster opening)")]
    public float lidDepth  = 1f;
    [Tooltip("Thickness of the auto-created lid")]
    public float lidThickness = 0.05f;
    [Tooltip("How high above this object's origin to place the lid hinge")]
    public float lidHeightOffset = 0.5f;

    [Header("Open Angle")]
    [Tooltip("How far the lid rotates open (degrees around its X axis)")]
    public float openAngle = 110f;

    [Header("Timing")]
    public float openSpeed  = 0.35f;
    public float closeDelay = 1.2f;
    public float closeSpeed = 0.5f;

    [Header("Audio (optional)")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;

    private Quaternion _closedRot;
    private Quaternion _openRot;
    private bool _isOpen;
    private int _weaponsInZone;
    private Coroutine _animCoroutine;
    private Coroutine _closeDelayCoroutine;

    void Awake()
    {
        lid ??= BuildDefaultLid();

        _closedRot = lid.localRotation;
        _openRot   = lid.localRotation * Quaternion.Euler(-openAngle, 0f, 0f);
    }

    private Transform BuildDefaultLid()
    {
        // Hinge sits at the back-top edge of the dumpster
        GameObject hinge = new GameObject("DumpsterLid_Hinge");
        hinge.transform.SetParent(transform, worldPositionStays: false);
        hinge.transform.localPosition = new Vector3(0f, lidHeightOffset, -lidDepth * 0.5f);

        // Visual lid mesh, pivots forward from the hinge
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.name = "DumpsterLid_Mesh";
        mesh.transform.SetParent(hinge.transform, worldPositionStays: false);
        mesh.transform.localPosition = new Vector3(0f, 0f, lidDepth * 0.5f);
        mesh.transform.localScale    = new Vector3(lidWidth, lidThickness, lidDepth);

        Destroy(mesh.GetComponent<Collider>());

        return hinge.transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Weapon")) return;

        _weaponsInZone++;

        if (_closeDelayCoroutine != null)
        {
            StopCoroutine(_closeDelayCoroutine);
            _closeDelayCoroutine = null;
        }

        SetLid(open: true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Weapon")) return;

        _weaponsInZone = Mathf.Max(0, _weaponsInZone - 1);

        if (_weaponsInZone == 0)
            _closeDelayCoroutine = StartCoroutine(DelayedClose());
    }

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
}
