namespace MuscleMemory.Services;

public sealed record ImportCounts(int Exercises, int Workouts, int Sessions)
{
    public static ImportCounts None { get; } = new(0, 0, 0);
}
