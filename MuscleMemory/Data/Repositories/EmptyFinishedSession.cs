namespace MuscleMemory.Data.Repositories;

internal static class EmptyFinishedSession
{
    public const string Condition = """
        WorkoutSession.EndTimeUtc IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM SessionExercise WHERE SessionExercise.WorkoutSessionId = WorkoutSession.Id)
        """;
}
