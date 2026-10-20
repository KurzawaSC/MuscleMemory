using MuscleMemory.Charting;

namespace MuscleMemory.Tests.Charting;

public sealed class NiceScaleTests
{
    private const int Seed = 20261019;
    private const int GeneratedRanges = 2000;
    private static readonly decimal[] NiceMantissas = [1m, 2m, 2.5m, 5m];

    [Fact]
    public void For_TypicalOneRepMaxRange_UsesTheSmallestStepWithAtMostFiveTicks()
    {
        var ticks = NiceScale.For(82.5, 100);

        Assert.Equal([80.0, 90, 100, 110], ticks.Values);
        Assert.Equal(0, ticks.Decimals);
    }

    [Fact]
    public void For_SinglePoint_PadsByATenthAroundIt()
    {
        var ticks = NiceScale.For(100, 100);

        Assert.Equal([90.0, 95, 100, 105, 110], ticks.Values);
    }

    [Fact]
    public void For_FlatZeroSeries_PadsByOne()
    {
        var ticks = NiceScale.For(0, 0);

        Assert.Equal([0.0, 0.25, 0.5, 0.75, 1], ticks.Values);
        Assert.Equal(2, ticks.Decimals);
    }

    [Fact]
    public void For_LargeVolumes_UsesThousandsStep()
    {
        var ticks = NiceScale.For(12000, 18500);

        Assert.Equal([10000.0, 12500, 15000, 17500, 20000], ticks.Values);
        Assert.Equal(0, ticks.Decimals);
    }

    [Fact]
    public void For_SmallDecimals_KeepsTheirDecimals()
    {
        var ticks = NiceScale.For(0.3, 0.9);

        Assert.Equal([0.2, 0.4, 0.6, 0.8, 1.0], ticks.Values);
        Assert.Equal(1, ticks.Decimals);
    }

    [Fact]
    public void For_QuarterSteps_HasTwoDecimals()
    {
        var ticks = NiceScale.For(1.1, 1.9);

        Assert.Equal(0.25, ticks.Values[1] - ticks.Values[0], 10);
        Assert.Equal(2, ticks.Decimals);
    }

    [Fact]
    public void For_NonNegativeValuesNearZero_NeverGoesBelowZero()
    {
        var ticks = NiceScale.For(1, 10);

        Assert.Equal(0, ticks.Minimum);
    }

    [Fact]
    public void For_NegativeValues_PadsBelowThem()
    {
        var ticks = NiceScale.For(-10, -2);

        Assert.True(ticks.Minimum < -10);
        Assert.True(ticks.Maximum >= -2);
    }

    [Fact]
    public void For_MinimumAboveMaximum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NiceScale.For(2, 1));
    }

    [Fact]
    public void For_GeneratedRanges_AlwaysThreeToFiveNiceEvenTicksCoveringTheValues()
    {
        var random = new Random(Seed);

        for (var index = 0; index < GeneratedRanges; index++)
        {
            var magnitude = Math.Pow(10, random.Next(-2, 6));
            var minimum = Math.Round(random.NextDouble() * magnitude, 2);
            var maximum = minimum + Math.Round(random.NextDouble() * magnitude, 2);

            var ticks = NiceScale.For(minimum, maximum);

            Assert.InRange(ticks.Values.Count, NiceScale.MinTicks, NiceScale.MaxTicks);
            Assert.True(ticks.Minimum <= minimum && ticks.Maximum >= maximum, $"{minimum}..{maximum} not covered by {ticks.Minimum}..{ticks.Maximum}");
            var step = (decimal)ticks.Values[1] - (decimal)ticks.Values[0];
            Assert.True(IsNice(step), $"step {step} for {minimum}..{maximum}");
            Assert.All(ticks.Values.Skip(1).Zip(ticks.Values), pair => Assert.Equal(step, (decimal)pair.First - (decimal)pair.Second));
        }
    }

    private static bool IsNice(decimal step)
    {
        while (step >= 10)
        {
            step /= 10;
        }

        while (step < 1)
        {
            step *= 10;
        }

        return NiceMantissas.Contains(step);
    }
}
