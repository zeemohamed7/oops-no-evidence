using UnityEngine;

public class GrabbableObject : MonoBehaviour
{
    [Header("State")]
    public bool isGrabbed = false;
    public GameObject currentHolder;

    public bool TryGrab(GameObject player)
    {
        // Already taken by someone else
        if (isGrabbed && currentHolder != player)
            return false;

        SetHolder(player);
        return true;
    }

    public void Release()
    {
        ClearHolder();
    }

    private void SetHolder(GameObject player)
    {
        isGrabbed = true;
        currentHolder = player;
    }

    private void ClearHolder()
    {
        isGrabbed = false;
        currentHolder = null;
    }
}
