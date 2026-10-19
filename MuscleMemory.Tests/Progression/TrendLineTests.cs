using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class TrendLineTests
{
    private static readonly DateOnly FirstDay = new(2026, 1, 5);

    [Fact]
    public void Fit_ThreeCollinearPoints_HasTheirSlope()
    {
        var trend = TrendLine.Fit([At(0, 100), At(16, 104), At(32, 108)]);

        Assert.Equal(0.25, trend?.SlopePerDay);
        Assert.Equal(7.5, trend?.PerMonth);
    }

    [Fact]
    public void Fit_ScatteredPoints_IsTheLeastSquaresLine()
    {
        var trend = TrendLine.Fit([At(0, 10), At(10, 30), At(20, 20), At(30, 40)]);

        Assert.Equal(0.8, trend?.SlopePerDay);
        Assert.Equal(FirstDay.DayNumber + 15, trend?.MeanDay);
        Assert.Equal(25, trend?.MeanValue);
        Assert.Equal(13, trend?.ValueAt(FirstDay.DayNumber));
    }

    [Fact]
    public void Fit_FallingValues_HasANegativeSlope()
    {
        var trend = TrendLine.Fit([At(0, 120), At(8, 118), At(16, 116)]);

        Assert.Equal(-0.25, trend?.SlopePerDay);
    }

    [Fact]
    public void Fit_EveryPointOnTheSameDay_HasNoTrend()
    {
        Assert.Null(TrendLine.Fit([At(3, 100), At(3, 110), At(3, 90)]));
    }

    [Fact]
    public void Fit_NoPoints_HasNoTrend()
    {
        Assert.Null(TrendLine.Fit([]));
    }

    [Fact]
    public void Find_ThreePointsSpanningFourteenDays_HasATrend()
    {
        Assert.NotNull(TrendLine.Find([At(0, 100), At(7, 101), At(14, 102)]));
    }

    [Fact]
    public void Find_SpanShorterThanFourteenDays_HasNoTrend()
    {
        Assert.Null(TrendLine.Find([At(0, 100), At(7, 101), At(13, 102)]));
    }

    [Fact]
    public void Find_TwoPointsFarApart_HasNoTrend()
    {
        Assert.Null(TrendLine.Find([At(0, 100), At(60, 110)]));
    }

    [Fact]
    public void Find_ManyPointsOnFewDays_CountsDaysNotPoints()
    {
        Assert.Null(TrendLine.Find([At(0, 100), At(0, 101), At(1, 102), At(1, 103)]));
    }

    [Fact]
    public void SpanDays_UnorderedPoints_IsFromTheEarliestToTheLatest()
    {
        Assert.Equal(30, TrendLine.SpanDays([At(10, 1), At(0, 1), At(30, 1)]));
    }

    private static ProgressPoint At(int day, double value) => new(day, FirstDay.AddDays(day), value);
}
