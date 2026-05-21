using UnityEngine;

public class PlayerPush : MonoBehaviour
{
    public float pushForce = 1f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null)
            return;

        FurnitureSnap snap = rb.GetComponent<FurnitureSnap>();

        if (snap != null)
        {
            if (snap.IsSolved)
                return;

            snap.EnterRearrangeMode();
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 pushDir =
            new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        rb.AddForce(pushDir * pushForce, ForceMode.VelocityChange);
    }
}