using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class ProgressTrendTests
{
    [Theory]
    [InlineData(0.25, 7.5)]
    [InlineData(-0.5, -15)]
    [InlineData(0, 0)]
    public void PerMonth_SlopePerDay_IsTheRateOverThirtyDays(double slopePerDay, double expected)
    {
        Assert.Equal(expected, new ProgressTrend(slopePerDay, 0, 0).PerMonth);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(104, 51)]
    [InlineData(96, 49)]
    public void ValueAt_Day_FollowsTheLineThroughTheMeans(double day, double expected)
    {
        Assert.Equal(expected, new ProgressTrend(0.25, 100, 50).ValueAt(day));
    }
}
