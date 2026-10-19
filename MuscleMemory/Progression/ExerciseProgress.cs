namespace MuscleMemory.Progression;

public sealed record ExerciseProgress(
    IReadOnlyList<ProgressPoint> OneRepMax,
    IReadOnlyList<ProgressPoint> Volume,
    IReadOnlyList<ProgressPoint> Reps)
{
    public IReadOnlyList<ProgressMetric> Metrics => [.. Enum.GetValues<ProgressMetric>().Where(metric => For(metric).Count > 0)];

    public bool HasAny => Metrics.Count > 0;

    public IReadOnlyList<ProgressPoint> For(ProgressMetric metric) => metric switch
    {
        ProgressMetric.OneRepMax => OneRepMax,
        ProgressMetric.Volume => Volume,
        ProgressMetric.Reps => Reps,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };
}
