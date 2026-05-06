using System;

public static class GameEvents
{
    // Suspicion
    public static Action<float> OnSuspicionAdded;
    public static Action<float> OnSuspicionReduced;

    // Tasks
    public static Action<string> OnTaskCompleted;

    // Optional (future)
    public static Action OnAllTasksCompleted;
}   