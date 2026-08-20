namespace MuscleMemory.Services;

public sealed record WorkoutHistorySession(
    int SessionId,
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    double TotalVolume,
    IReadOnlyList<WorkoutHistoryExercise> Exercises)
{
    public DateTime LocalStartTime => StartTimeUtc.ToLocalTime();

    public TimeSpan Duration => EndTimeUtc.HasValue ? EndTimeUtc.Value - StartTimeUtc : TimeSpan.Zero;
}
