namespace MuscleMemory.Progression;

public sealed record ExerciseRecords(
    PerformedSet? HeaviestWeight,
    OneRepMaxRecord? BestOneRepMax,
    SessionVolume? BestSessionVolume,
    PerformedSet? MostReps)
{
    public bool HasAny => HeaviestWeight is not null || BestOneRepMax is not null || BestSessionVolume is not null || MostReps is not null;
}
