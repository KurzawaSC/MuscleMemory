namespace MuscleMemory.Progression;

public static class TrendLine
{
    public const int MinPoints = 3;
    public const int MinSpanDays = 14;

    public static ProgressTrend? Find(IReadOnlyList<ProgressPoint> points) =>
        points.Count >= MinPoints && SpanDays(points) >= MinSpanDays ? Fit(points) : null;

    public static ProgressTrend? Fit(IReadOnlyList<ProgressPoint> points)
    {
        if (points.Count == 0)
        {
            return null;
        }

        var meanDay = points.Average(point => (double)point.Day);
        var meanValue = points.Average(point => point.Value);
        var spread = points.Sum(point => Square(point.Day - meanDay));

        if (spread == 0)
        {
            return null;
        }

        var covariance = points.Sum(point => (point.Day - meanDay) * (point.Value - meanValue));
        return new ProgressTrend(covariance / spread, meanDay, meanValue);
    }

    public static int SpanDays(IReadOnlyList<ProgressPoint> points) =>
        points.Count == 0 ? 0 : points.Max(point => point.Day) - points.Min(point => point.Day);

    private static double Square(double value) => value * value;
}
