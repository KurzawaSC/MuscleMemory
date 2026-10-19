namespace MuscleMemory.Progression;

public sealed record SessionRepTotals
{
    public int SessionId { get; init; }
    public DateTime DateUtc { get; init; }
    public int Reps { get; init; }
    public double HeaviestWeight { get; init; }
    public double Volume { get; init; }

    public bool IsBodyweight => HeaviestWeight <= PerformedSet.BodyweightLoad;

    public double? EstimatedOneRepMax => OneRepMax.Estimate(HeaviestWeight, Reps);
}
