using System.Diagnostics;

namespace MuscleMemory.Charting;

public static class NiceScale
{
    public const int MinTicks = 3;
    public const int MaxTicks = 5;
    public const double PaddingFraction = 0.05;
    public const double FlatPaddingFraction = 0.1;
    public const double ZeroPadding = 1;

    private const int Base = 10;
    private static readonly decimal[] Mantissas = [1m, 2m, 2.5m, 5m];

    public static ValueTicks For(double minimum, double maximum)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimum, maximum);

        var (low, high) = Padded(minimum, maximum);

        foreach (var step in StepsFrom(high - low))
        {
            var first = Math.Floor(low / step) * step;
            var last = Math.Ceiling(high / step) * step;
            var count = (int)((last - first) / step) + 1;

            if (count <= MaxTicks)
            {
                return new ValueTicks([.. Enumerable.Range(0, count).Select(index => (double)(first + (index * step)))], DecimalsOf(step));
            }
        }

        throw new UnreachableException();
    }

    private static (decimal Low, decimal High) Padded(double minimum, double maximum)
    {
        var padding = maximum > minimum ? (maximum - minimum) * PaddingFraction : FlatPadding(maximum);
        var low = minimum >= 0 ? Math.Max(minimum - padding, 0) : minimum - padding;
        return ((decimal)low, (decimal)(maximum + padding));
    }

    private static double FlatPadding(double value) => value == 0 ? ZeroPadding : Math.Abs(value) * FlatPaddingFraction;

    private static IEnumerable<decimal> StepsFrom(decimal span)
    {
        var exponent = (int)Math.Floor(Math.Log10((double)span / MaxTicks)) - 1;

        for (var magnitude = PowerOfTen(exponent); ; magnitude *= Base)
        {
            foreach (var mantissa in Mantissas)
            {
                yield return mantissa * magnitude;
            }
        }
    }

    private static int DecimalsOf(decimal step)
    {
        var decimals = 0;

        for (var scaled = step; scaled % 1 != 0; scaled *= Base)
        {
            decimals++;
        }

        return decimals;
    }

    private static decimal PowerOfTen(int exponent)
    {
        var power = 1m;

        for (var index = 0; index < Math.Abs(exponent); index++)
        {
            power = exponent < 0 ? power / Base : power * Base;
        }

        return power;
    }
}
