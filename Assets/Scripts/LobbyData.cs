// Static class — no MonoBehaviour, no scene.
// Persists between scenes because it lives in static memory.
// LobbyManager writes into it; PlayerSpawner and CharacterSelector read from it.
public static class LobbyData
{
    public const int MaxPlayers    = 4;
    public const int CharacterCount = 5; // 0 = Default, 1-4 = named characters

    // GDD characters: one default slot + the 4 named "Fixers"
    public static readonly string[] CharacterNames =
    {
        "Default",  // 0 — assigned automatically when a player first joins
        "Mariam",   // 1
        "Hajar",    // 2
        "Zainab",   // 3
        "Malak",    // 4
        "Noora"     // 5  (optional 5th named character; set CharacterCount = 6 to include)
    };

    // -1 means the slot is empty (player has not joined)
    public static int[] PlayerCharacters = new int[MaxPlayers] { -1, -1, -1, -1 };

    public static int PlayerCount = 0;

    // Call this before loading a fresh lobby to wipe any stale data.
    public static void Reset()
    {
        for (int i = 0; i < MaxPlayers; i++)
            PlayerCharacters[i] = -1;
        PlayerCount = 0;
    }

    // Returns true if charIndex is already chosen by any joined player.
    public static bool IsCharacterTaken(int charIndex)
    {
        for (int i = 0; i < MaxPlayers; i++)
            if (PlayerCharacters[i] == charIndex) return true;
        return false;
    }

    // Returns the character index for a player, or -1 if not joined.
    public static int GetCharacter(int playerIndex) => PlayerCharacters[playerIndex];

    // Returns how many players have joined (slot != -1).
    public static int JoinedCount()
    {
        int count = 0;
        for (int i = 0; i < MaxPlayers; i++)
            if (PlayerCharacters[i] != -1) count++;
        return count;
    }
}
