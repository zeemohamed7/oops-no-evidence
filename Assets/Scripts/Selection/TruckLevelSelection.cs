using UnityEngine;

public class TruckLevelSelection : MonoBehaviour
{
    public float driveSpeed = 8f;
    public Vector2 offset; // truck is leaning, add offset to stay on t rack

    private RectTransform truckRect;
    private Vector2 targetPos;

    void Awake()
    {
        truckRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Lerp calculates a point between Point A and Point B.
        // Over time, this creates a smooth "slide" effect.
        truckRect.anchoredPosition = Vector2.Lerp(
            truckRect.anchoredPosition, 
            targetPos, 
            Time.deltaTime * driveSpeed
        );
    }

    public void SetTarget(RectTransform targetCard)
    {
        targetPos = targetCard.anchoredPosition + offset;
    }
}