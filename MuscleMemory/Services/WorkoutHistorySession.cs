namespace MuscleMemory.Services;

public sealed record WorkoutHistorySession(
    int SessionId,
    DateTime StartTime,
    DateTime? EndTime,
    double TotalVolume,
    IReadOnlyList<WorkoutHistoryExercise> Exercises)
{
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
}
