using UnityEngine;

public class WeaponDisposalZone : MonoBehaviour
{
    private bool completed;

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;

        if (other.CompareTag("Weapon"))
        {
            completed = true;

            GameEvents.OnTaskCompleted?.Invoke("dispose_weapon");

            Destroy(other.gameObject);

            Debug.Log("Weapon disposed");
        }
    }
}