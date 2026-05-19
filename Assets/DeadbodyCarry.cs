using System.Collections.Generic;
using UnityEngine;

public class DeadbodyCarry : MonoBehaviour
{
    // Track players currently carrying this specific body
    private readonly List<GameObject> holdingPlayers = new();

    public void RegisterPlayer(GameObject player)
    {
        if (!holdingPlayers.Contains(player))
        {
            holdingPlayers.Add(player);
            UpdateBodyWeight();
        }
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (holdingPlayers.Contains(player))
        {
            holdingPlayers.Remove(player);
            UpdateBodyWeight();
        }
    }

    private void UpdateBodyWeight()
    {
        int carrierCount = holdingPlayers.Count;
        if (carrierCount == 0) return;

        // Determine the speed multiplier based on player count
        // 1 Player holding = 0.5f (Moves at 50% speed)
        // 2+ Players holding = 0.85f (Moves at 85% speed - much lighter!)
        float dynamicMultiplier = (carrierCount > 1) ? 0.85f : 0.5f;

        // Push this updated speed scaling down to every active carrier
        foreach (GameObject player in holdingPlayers)
        {
            TopDownPlayerController controller = player.GetComponent<TopDownPlayerController>();
            if (controller != null)
            {
                controller.SetCarryWeight(dynamicMultiplier);
            }
        }
    }
}