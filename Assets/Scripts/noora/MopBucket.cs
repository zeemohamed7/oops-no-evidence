using UnityEngine;

public class MopBucket : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        MopCleaner mop = other.GetComponent<MopCleaner>();

        if (mop != null)
        {
            mop.ResetMop();
        }
    }
}