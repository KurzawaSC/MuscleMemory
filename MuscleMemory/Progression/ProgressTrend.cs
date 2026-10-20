namespace MuscleMemory.Progression;

public sealed record ProgressTrend(double SlopePerDay, double MeanDay, double MeanValue)
{
    public const int DaysPerMonth = 30;

    public double PerMonth => SlopePerDay * DaysPerMonth;

    public double ValueAt(double day) => MeanValue + (SlopePerDay * (day - MeanDay));
}
