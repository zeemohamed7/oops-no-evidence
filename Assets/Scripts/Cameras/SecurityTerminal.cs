using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecurityTerminal : MonoBehaviour
{
    [Header("Hacking Settings")]
    public float hackDuration = 15f;
    public string completionTaskId = "hack_cameras";

    [Header("Cameras to Disable on Completion")]
    public SecurityCamera[] targetCameras;

    [Header("World Space UI")]
    public GameObject progressBarRoot;
    public Image progressFill;
    public TextMeshProUGUI statusText;
    public bool billboardBar = true;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip typingSound;       // looped while hacking
    public AudioClip cameraOffSound;    // played on completion

    private float _progress;
    private bool  _complete;
    private bool  _hacking;

    public bool IsComplete => _complete;

    private void Start()
    {
        if (progressBarRoot != null)
            progressBarRoot.SetActive(true);

        UpdateBar();
        SetStatus("PRESS E TO HACK");
    }

    private void Update()
    {
        if (billboardBar && progressBarRoot != null)
        {
            progressBarRoot.transform.LookAt(Camera.main.transform);
            progressBarRoot.transform.Rotate(0f, 0f, 0f);
        }
    }

    public void AddProgress(float delta)
    {
        if (_complete) return;

        // Start typing sound when hacking begins
        if (!_hacking)
        {
            _hacking = true;
            if (audioSource != null && typingSound != null)
            {
                audioSource.clip   = typingSound;
                audioSource.loop   = true;
                audioSource.Play();
            }
        }

        _progress += delta;
        _progress  = Mathf.Clamp(_progress, 0f, hackDuration);

        UpdateBar();
        SetStatus("HACKING...");

        if (_progress >= hackDuration)
            CompleteHack();
    }

    public void ResetProgress()
    {
        if (_complete) return;

        // Stop typing sound on pause
        if (_hacking)
        {
            _hacking = false;
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        SetStatus("PRESS E TO HACK");
    }

    private void CompleteHack()
    {
        _complete = true;
        _hacking  = false;

        // Stop typing, play camera off sound
        if (audioSource != null)
        {
            audioSource.Stop();
            if (cameraOffSound != null)
                audioSource.PlayOneShot(cameraOffSound);
        }

        UpdateBar();
        SetStatus("ACCESS GRANTED");

        GameEvents.OnTaskCompleted?.Invoke(completionTaskId);

        foreach (var cam in targetCameras)
        {
            if (cam == null) continue;
            cam.enabled = false;
            var cone = cam.transform.Find("CameraVisionCone");
            if (cone != null) cone.gameObject.SetActive(false);
        }
    }

    private void UpdateBar()
    {
        if (progressFill != null)
            progressFill.fillAmount = _progress / hackDuration;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}