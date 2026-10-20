namespace MuscleMemory.Progression;

public static class ProgressRanges
{
    public const int ThreeMonths = 3;
    public const int SixMonths = 6;
    public const int TwelveMonths = 12;

    public static DateOnly? StartOf(ProgressRange range, DateOnly today) => range switch
    {
        ProgressRange.ThreeMonths => today.AddMonths(-ThreeMonths),
        ProgressRange.SixMonths => today.AddMonths(-SixMonths),
        ProgressRange.TwelveMonths => today.AddMonths(-TwelveMonths),
        ProgressRange.All => null,
        _ => throw new ArgumentOutOfRangeException(nameof(range), range, null)
    };

    public static IReadOnlyList<ProgressPoint> Filter(IReadOnlyList<ProgressPoint> points, ProgressRange range, DateOnly today) =>
        StartOf(range, today) is { } start ? [.. points.Where(point => point.Date >= start)] : points;
}
