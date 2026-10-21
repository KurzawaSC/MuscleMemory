namespace MuscleMemory.Progression;

public static class DoubleProgression
{
    public const int RepIncrement = 1;

    public static ProgressionSuggestion? Suggest(
        RepRange? range,
        int plannedSets,
        IReadOnlyList<PerformedSet> lastSession,
        IReadOnlyList<PerformedSet> previousSession,
        double weightStep)
    {
        if (range is null || WorkingSets(lastSession) is not [var heaviest, ..] working)
        {
            return null;
        }

        var weight = heaviest.Weight;

        if (heaviest.IsBodyweight)
        {
            return Hold(weight, working.Select(set => set.Reps + RepIncrement), plannedSets, range);
        }

        if (IsIncrease(range, plannedSets, working))
        {
            return new ProgressionSuggestion(SuggestionOutcome.Increase, weight + weightStep, AtBottomOfRange(range, plannedSets));
        }

        if (IsDecrease(range, working, previousSession, weight, weightStep))
        {
            return new ProgressionSuggestion(SuggestionOutcome.Decrease, weight - weightStep, AtBottomOfRange(range, plannedSets));
        }

        return Hold(weight, working.Select(set => Math.Min(set.Reps + RepIncrement, range.Max)), plannedSets, range);
    }

    public static IReadOnlyList<PerformedSet> WorkingSets(IEnumerable<PerformedSet> session)
    {
        List<PerformedSet> eligible = [.. session.Where(PersonalRecords.IsEligible)];

        if (eligible.Count == 0)
        {
            return [];
        }

        var workingWeight = eligible.Max(set => set.Weight);
        return [.. eligible.Where(set => set.Weight == workingWeight).OrderBy(set => set.SetId)];
    }

    private static bool IsIncrease(RepRange range, int plannedSets, IReadOnlyList<PerformedSet> working) =>
        working.Count >= plannedSets && working.All(set => set.Reps >= range.Max);

    private static bool IsDecrease(RepRange range, IReadOnlyList<PerformedSet> working, IReadOnlyList<PerformedSet> previousSession, double weight, double weightStep) =>
        IsBelowRange(range, working)
        && WorkingSets(previousSession) is [var previousHeaviest, ..] previousWorking
        && previousHeaviest.Weight == weight
        && IsBelowRange(range, previousWorking)
        && weight - weightStep > PerformedSet.BodyweightLoad;

    private static bool IsBelowRange(RepRange range, IReadOnlyList<PerformedSet> working) =>
        working.All(set => set.Reps < range.Min);

    private static int[] AtBottomOfRange(RepRange range, int plannedSets) => [.. Enumerable.Repeat(range.Min, plannedSets)];

    private static ProgressionSuggestion Hold(double weight, IEnumerable<int> workingTargets, int plannedSets, RepRange range)
    {
        List<int> targets = [.. workingTargets];
        targets.AddRange(Enumerable.Repeat(range.Min, Math.Max(plannedSets - targets.Count, 0)));

        return new ProgressionSuggestion(SuggestionOutcome.Hold, weight, targets);
    }
}
