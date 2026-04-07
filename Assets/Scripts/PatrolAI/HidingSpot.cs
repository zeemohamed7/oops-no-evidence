using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    public Transform hidingPoint;
    public int hiddenLayer; // Player's targety layer changes to "Default"
    private bool isOccupied;

    private int originalLayer;

    public void ToggleHide(GameObject player)
    {
        isOccupied = !isOccupied;

        if (isOccupied)
        {
            originalLayer = player.layer;
            player.layer = hiddenLayer;

            if (hidingPoint != null)
                player.transform.position = hidingPoint.position;
        }
        else
        {
            player.layer = originalLayer;
        }
    }
}