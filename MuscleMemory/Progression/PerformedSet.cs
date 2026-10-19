namespace MuscleMemory.Progression;

public sealed record PerformedSet
{
    public const double BodyweightLoad = 0;

    public int SetId { get; init; }
    public int SessionId { get; init; }
    public int ExerciseId { get; init; }
    public DateTime DateUtc { get; init; }
    public double Weight { get; init; }
    public int Reps { get; init; }

    public bool IsBodyweight => Weight <= BodyweightLoad;

    public double? EstimatedOneRepMax => OneRepMax.Estimate(Weight, Reps);
}
