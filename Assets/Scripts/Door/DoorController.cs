using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Animator anim; 

    [Header("Settings")]
    public bool isSwingingDoor = true;
    public bool openOnlyOnce = false;
    
    private bool _hasOpened = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Check if we already opened and it's a "once only" door
            if (openOnlyOnce && _hasOpened) return;

            if (isSwingingDoor && anim != null)
            {
                // 2. Bidirectional Logic: Check if player is in front/behind
                Vector3 dirToPlayer = other.transform.position - transform.position;
                float dot = Vector3.Dot(transform.forward, dirToPlayer);

                // Set 1 for away, -1 for toward (Adjust based on your animation)
                anim.SetFloat("swingDirection", dot > 0 ? 1f : -1f);
            }

            if (anim != null)
            {
                anim.SetBool("isOpen", true);
                _hasOpened = true;
                Debug.Log("Door: Opening");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 3. Only close if it's NOT a "once only" door
        if (other.CompareTag("Player") && !openOnlyOnce)
        {
            if (anim != null)
            {
                anim.SetBool("isOpen", false);
                Debug.Log("Door: Closing");
            }
        }
    }
}