using System;

public static class GameEvents
{
    // Suspicion
    public static Action<float> OnSuspicionAdded;
    public static Action<float> OnSuspicionReduced;

    // Tasks
    public static Action<string> OnTaskCompleted;

    // Carrying — true = body, false = weapon
    public static Action<bool> OnCarryStart;
    public static Action       OnCarryStop;

    // Optional (future)
    public static Action OnAllTasksCompleted;
}