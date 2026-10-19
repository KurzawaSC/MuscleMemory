namespace MuscleMemory.Charting;

public readonly record struct LinearScale(double DomainStart, double DomainEnd, float RangeStart, float RangeEnd)
{
    private const float Half = 0.5f;

    public float Map(double value) =>
        DomainEnd == DomainStart
            ? RangeStart + ((RangeEnd - RangeStart) * Half)
            : RangeStart + ((float)((value - DomainStart) / (DomainEnd - DomainStart)) * (RangeEnd - RangeStart));
}
