using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    public Transform hidePosition;
    private bool isOccupied;
    private Vector3 playerReturnPos; // To save player originally was

    public void ToggleHide(GameObject player)
    {
        var controller = player.GetComponent<TopDownPlayerController>();
        // Get ALL visual parts so nothing stays visible
        var renderers = player.GetComponentsInChildren<Renderer>();

        if (!isOccupied)
        {
            // --- ENTERING HIDING ---

            playerReturnPos = player.transform.position; // Save current position to put player back

            if (player.TryGetComponent(out CharacterController cc))
                cc.enabled = false; // Disable unity character controller
            if (player.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = true; // Ignore gravity and collisions for a bit

            player.transform.position = hidePosition.position;
            player.transform.rotation = hidePosition.rotation;

            foreach (var r in renderers) r.enabled = false;

            controller.enabled = false; // Turns off TopDownPlayerController
            player.layer = LayerMask.NameToLayer("Ignore Raycast");
            isOccupied = true;
        }
        else
        {
            // --- EXITING HIDING ---

            if (player.TryGetComponent(out CharacterController cc))
                cc.enabled = true; // Turns physics collision back on
            if (player.TryGetComponent(out Rigidbody rb))
                rb.isKinematic = false; // Gives player back to physics engine so gravity can work again

            foreach (var r in renderers) r.enabled = true;

            // Return to the exact spot we were standing before hiding
            player.transform.position = playerReturnPos;

            controller.enabled = true;


            player.layer = LayerMask.NameToLayer("Target");

            isOccupied = false;
        }
    }
}