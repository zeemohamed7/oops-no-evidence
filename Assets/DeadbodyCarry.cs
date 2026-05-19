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
    public int GetCarrierCount() => holdingPlayers.Count;

    public GameObject GetOtherPlayer(GameObject localPlayer)
    {
        foreach (GameObject player in holdingPlayers)
        {
            if (player != localPlayer) return player;
        }
        return null;
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (holdingPlayers.Contains(player))
        {
            holdingPlayers.Remove(player);
            
            // 🟢 FIX PART 1: Reset the dropping player's movement parameters immediately
            TopDownPlayerController controller = player.GetComponent<TopDownPlayerController>();
            if (controller != null)
            {
                controller.ClearCarryPenalty(); // Explicitly clear penalty back to 1.0f base walk speed!
            }

            UpdateBodyWeight();
        }
    }

    // 🟢 FIX PART 2: Separated this method out so Grab.cs can query the list size safely at runtime
    public float GetPenaltyForPlayerCount()
    {
        int carrierCount = holdingPlayers.Count;
        return (carrierCount > 1) ? 0.85f : 0.5f;
    }

    public void UpdateBodyWeight()
    {
        // 🟢 FIX PART 3: If no one is holding it anymore, we just stop safely because UnregisterPlayer handled the clean up!
        if (holdingPlayers.Count == 0) return;

        // Determine the speed multiplier based on active player list count dynamically
        float dynamicMultiplier = GetPenaltyForPlayerCount();

        // Push this updated speed scaling down to every active carrier remaining on the body
        foreach (GameObject player in holdingPlayers)
        {
            if (player != null)
            {
                TopDownPlayerController controller = player.GetComponent<TopDownPlayerController>();
                if (controller != null)
                {
                    controller.SetCarryWeight(dynamicMultiplier);
                }
            }
        }
    }
}