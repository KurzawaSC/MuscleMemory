namespace MuscleMemory.Progression;

public static class ProgressSeries
{
    public static ExerciseProgress Build(IEnumerable<SessionRepTotals> totals, TimeZoneInfo localZone)
    {
        List<IGrouping<int, SessionRepTotals>> sessions =
        [
            .. totals.Where(total => total.Reps >= PersonalRecords.MinReps)
                     .GroupBy(total => total.SessionId)
                     .OrderBy(session => session.First().DateUtc)
                     .ThenBy(session => session.Key)
        ];
        var isBodyweightOnly = sessions.Count > 0 && sessions.All(session => session.All(total => total.IsBodyweight));

        return new ExerciseProgress(
            Points(sessions, localZone, BestOneRepMax),
            Points(sessions, localZone, SessionVolume),
            isBodyweightOnly ? Points(sessions, localZone, MostReps) : []);
    }

    private static List<ProgressPoint> Points(
        List<IGrouping<int, SessionRepTotals>> sessions,
        TimeZoneInfo localZone,
        Func<IGrouping<int, SessionRepTotals>, double?> valueOf) =>
        [
            .. sessions.SelectMany(session => valueOf(session) is { } value
                ? [new ProgressPoint(session.Key, LocalDate(session.First().DateUtc, localZone), value)]
                : Array.Empty<ProgressPoint>())
        ];

    private static double? BestOneRepMax(IGrouping<int, SessionRepTotals> session) =>
        session.Max(total => total.EstimatedOneRepMax);

    private static double? SessionVolume(IGrouping<int, SessionRepTotals> session) =>
        session.Sum(total => total.Volume) is var volume && volume > 0 ? volume : null;

    private static double? MostReps(IGrouping<int, SessionRepTotals> session) =>
        session.Max(total => total.Reps);

    private static DateOnly LocalDate(DateTime dateUtc, TimeZoneInfo localZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(dateUtc, localZone));
}
