using UnityEngine;

public class BucketSelectSound : MonoBehaviour
{
    [Header("References")]
    public ToolInventory inventory;

    [Header("Bucket Select Sound")]
    public AudioSource bucketSoundSource;

    private bool wasBucketSelected;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<ToolInventory>();

        if (bucketSoundSource != null)
        {
            bucketSoundSource.playOnAwake = false;
            bucketSoundSource.loop = false;
        }

        if (inventory != null)
            wasBucketSelected = inventory.IsBucketSelected();
    }

    void Update()
    {
        if (inventory == null || bucketSoundSource == null) return;

        bool isBucketSelected = inventory.IsBucketSelected();

        // Play once only when the player switches to the bucket
        if (isBucketSelected && !wasBucketSelected)
        {
            bucketSoundSource.Play();
        }

        wasBucketSelected = isBucketSelected;
    }
}
