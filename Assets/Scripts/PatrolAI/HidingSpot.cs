using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    public Transform hidePosition;
    private bool isOccupied;
    private Vector3 playerReturnPos; // To save player originally was

    [Header("Audio Settings")]
    [Tooltip("Sound that plays when entering the hiding spot")]
    public AudioClip enterSound;
    [Tooltip("Sound that plays when leaving the hiding spot")]
    public AudioClip exitSound;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // Clean 2D sound
    }

    public void ToggleHide(GameObject player)
    {
        var controller = player.GetComponent<TopDownPlayerController>();
        var renderers = player.GetComponentsInChildren<Renderer>();

        if (!isOccupied)
        {
            // --- ENTERING HIDING ---

            playerReturnPos = player.transform.position; 

            // // 🛑 FIX: Silence the footsteps on the player before freezing the controller component
            // if (controller != null && controller.footstepSource != null)
            // {
            //     controller.footstepSource.Stop();
            // }

            if (player.TryGetComponent(out CharacterController cc))
                cc.enabled = false; 
            if (player.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true; 

            player.transform.position = hidePosition.position;
            player.transform.rotation = hidePosition.rotation;

            foreach (var r in renderers) r.enabled = false;

            controller.enabled = false; // Turns off TopDownPlayerController smoothly now!
            player.layer = LayerMask.NameToLayer("Ignore Raycast");

            if (_audioSource != null && enterSound != null)
            {
                _audioSource.PlayOneShot(enterSound);
            }

            isOccupied = true;
        }
        else
        {
            // --- EXITING HIDING ---

            if (player.TryGetComponent(out CharacterController cc))
                cc.enabled = true; 
            if (player.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = false; 

            foreach (var r in renderers) r.enabled = true;

            player.transform.position = playerReturnPos;

            controller.enabled = true;

            player.layer = LayerMask.NameToLayer("Target");

            if (_audioSource != null && exitSound != null)
            {
                _audioSource.PlayOneShot(exitSound);
            }

            isOccupied = false;
        }
    }
}