namespace MuscleMemory.Progression;

internal readonly record struct RunningBest(double? Weight, double? EstimatedOneRepMax, int? BodyweightReps)
{
    public static RunningBest Of(IReadOnlyCollection<PerformedSet> history) =>
        new(
            history.Max(LoadOf),
            history.Max(set => set.EstimatedOneRepMax),
            history.All(set => set.IsBodyweight) ? history.Max(BodyweightRepsOf) : null);

    public bool IsBeatenBy(PerformedSet set) =>
        Exceeds(Weight, LoadOf(set))
        || Exceeds(EstimatedOneRepMax, set.EstimatedOneRepMax)
        || Exceeds(BodyweightReps, BodyweightRepsOf(set));

    public RunningBest Including(PerformedSet set) =>
        new(
            Raise(Weight, LoadOf(set)),
            Raise(EstimatedOneRepMax, set.EstimatedOneRepMax),
            Raise(BodyweightReps, BodyweightRepsOf(set)));

    private static double? LoadOf(PerformedSet set) => set.IsBodyweight ? null : set.Weight;

    private static int? BodyweightRepsOf(PerformedSet set) => set.IsBodyweight ? set.Reps : null;

    private static bool Exceeds<T>(T? best, T? value) where T : struct, IComparable<T> =>
        best is { } current && value is { } candidate && candidate.CompareTo(current) > 0;

    private static T? Raise<T>(T? best, T? value) where T : struct, IComparable<T> =>
        Exceeds(best, value) ? value : best;
}
