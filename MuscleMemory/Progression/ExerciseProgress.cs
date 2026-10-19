namespace MuscleMemory.Progression;

public sealed record ExerciseProgress(
    IReadOnlyList<ProgressPoint> OneRepMax,
    IReadOnlyList<ProgressPoint> Volume,
    IReadOnlyList<ProgressPoint> Reps)
{
    public IReadOnlyList<ProgressMetric> Metrics => [.. Enum.GetValues<ProgressMetric>().Where(metric => For(metric).Count > 0)];

    public bool HasAny => Metrics.Count > 0;

    public static IReadOnlyList<ProgressMetric> MetricsFor(IEnumerable<RecordKind> recordKinds) =>
    [
        .. recordKinds.SelectMany(kind => kind switch
        {
            RecordKind.BestOneRepMax => [ProgressMetric.OneRepMax],
            RecordKind.BestSessionVolume => [ProgressMetric.Volume],
            RecordKind.MostReps => [ProgressMetric.Reps],
            _ => Array.Empty<ProgressMetric>()
        }).Order()
    ];

    public IReadOnlyList<ProgressPoint> For(ProgressMetric metric) => metric switch
    {
        ProgressMetric.OneRepMax => OneRepMax,
        ProgressMetric.Volume => Volume,
        ProgressMetric.Reps => Reps,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };
}
