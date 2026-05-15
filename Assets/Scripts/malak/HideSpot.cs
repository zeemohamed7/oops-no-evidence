using UnityEngine;

public class HideSpot : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerHiding playerHiding = other.GetComponent<PlayerHiding>();

        if (playerHiding != null)
        {
            playerHiding.EnterHide();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHiding playerHiding = other.GetComponent<PlayerHiding>();

        if (playerHiding != null)
        {
            playerHiding.ExitHide();
        }
    }
}
