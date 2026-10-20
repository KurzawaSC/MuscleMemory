namespace MuscleMemory.Progression;

public sealed record RepRange
{
    public const int Lowest = 1;
    public const int Highest = 30;

    private RepRange(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public int Min { get; }

    public int Max { get; }

    public static bool IsValid(int min, int max) => min >= Lowest && min <= max && max <= Highest;

    public static RepRange? From(int? min, int? max) =>
        min is int lower && max is int upper && IsValid(lower, upper) ? new RepRange(lower, upper) : null;
}
