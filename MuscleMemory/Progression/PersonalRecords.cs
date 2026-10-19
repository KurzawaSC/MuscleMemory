namespace MuscleMemory.Progression;

public static class PersonalRecords
{
    public const int MinReps = 1;

    public static bool IsEligible(PerformedSet set) => set.Reps >= MinReps;

    public static ExerciseRecords Find(IEnumerable<PerformedSet> sets, IEnumerable<SessionVolume> sessionVolumes)
    {
        List<PerformedSet> eligible = [.. sets.Where(IsEligible)];

        return new ExerciseRecords(
            HeaviestWeight(eligible),
            BestOneRepMax(eligible),
            BestSessionVolume(sessionVolumes),
            MostReps(eligible));
    }

    public static IReadOnlySet<int> FindNewRecords(IEnumerable<PerformedSet> history, IEnumerable<PerformedSet> session)
    {
        var bests = history.Where(IsEligible)
                           .GroupBy(set => set.ExerciseId)
                           .ToDictionary(group => group.Key, group => RunningBest.Of([.. group]));
        HashSet<int> newRecords = [];

        foreach (var set in session.Where(IsEligible).OrderBy(set => set.SetId))
        {
            if (!bests.TryGetValue(set.ExerciseId, out var best))
            {
                continue;
            }

            if (best.IsBeatenBy(set))
            {
                newRecords.Add(set.SetId);
            }

            bests[set.ExerciseId] = best.Including(set);
        }

        return newRecords;
    }

    private static PerformedSet? HeaviestWeight(List<PerformedSet> sets) =>
        sets.Where(set => !set.IsBodyweight)
            .OrderByDescending(set => set.Weight)
            .ThenByDescending(set => set.Reps)
            .ThenBy(set => set.DateUtc)
            .ThenBy(set => set.SetId)
            .FirstOrDefault();

    private static OneRepMaxRecord? BestOneRepMax(List<PerformedSet> sets) =>
        sets.SelectMany(set => set.EstimatedOneRepMax is { } value ? [new OneRepMaxRecord(value, set)] : Array.Empty<OneRepMaxRecord>())
            .OrderByDescending(record => record.Value)
            .ThenBy(record => record.Set.DateUtc)
            .ThenBy(record => record.Set.SetId)
            .FirstOrDefault();

    private static SessionVolume? BestSessionVolume(IEnumerable<SessionVolume> sessionVolumes) =>
        sessionVolumes.Where(session => session.Volume > 0)
                      .OrderByDescending(session => session.Volume)
                      .ThenBy(session => session.DateUtc)
                      .ThenBy(session => session.SessionId)
                      .FirstOrDefault();

    private static PerformedSet? MostReps(List<PerformedSet> sets) =>
        sets.Count > 0 && sets.All(set => set.IsBodyweight)
            ? sets.OrderByDescending(set => set.Reps).ThenBy(set => set.DateUtc).ThenBy(set => set.SetId).First()
            : null;
}
