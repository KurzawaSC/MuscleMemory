namespace MuscleMemory.Charting;

public sealed record ValueTicks(IReadOnlyList<double> Values, int Decimals)
{
    public double Minimum => Values[0];

    public double Maximum => Values[^1];
}
