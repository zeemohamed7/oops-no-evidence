using System.Collections.Generic;
using UnityEngine;

public class LobbyDataCarrier : MonoBehaviour
{
    public static LobbyDataCarrier Instance;

    // This holds onto the player choices safely across scenes
    public List<PlayerSelectionData> playersToSpawn = new();
    public int currentLevelIndex = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // This carrier stays alive!
        }
        else
        {
            Destroy(gameObject);
        }
    }
}