namespace MuscleMemory.Charting;

public sealed record ChartModel(
    IReadOnlyList<ChartPoint> Points,
    double FirstX,
    double LastX,
    IReadOnlyList<AxisTick> XTicks,
    IReadOnlyList<AxisTick> YTicks,
    ChartPoint? TrendStart,
    ChartPoint? TrendEnd)
{
    public double MinimumY => YTicks[0].Value;

    public double MaximumY => YTicks[^1].Value;
}
