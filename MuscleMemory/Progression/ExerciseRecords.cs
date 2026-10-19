namespace MuscleMemory.Progression;

public sealed record ExerciseRecords(
    PerformedSet? HeaviestWeight,
    OneRepMaxRecord? BestOneRepMax,
    SessionVolume? BestSessionVolume,
    PerformedSet? MostReps)
{
    public bool HasAny => HeaviestWeight is not null || BestOneRepMax is not null || BestSessionVolume is not null || MostReps is not null;

    public IReadOnlyList<RecordKind> Kinds => [.. FoundKinds()];

    private IEnumerable<RecordKind> FoundKinds()
    {
        if (HeaviestWeight is not null)
        {
            yield return RecordKind.HeaviestWeight;
        }

        if (BestOneRepMax is not null)
        {
            yield return RecordKind.BestOneRepMax;
        }

        if (BestSessionVolume is not null)
        {
            yield return RecordKind.BestSessionVolume;
        }

        if (MostReps is not null)
        {
            yield return RecordKind.MostReps;
        }
    }
}
