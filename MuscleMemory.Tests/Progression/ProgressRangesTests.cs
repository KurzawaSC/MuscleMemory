using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class ProgressRangesTests
{
    private static readonly DateOnly Today = new(2026, 10, 19);

    [Theory]
    [InlineData(ProgressRange.ThreeMonths, 2026, 7, 19)]
    [InlineData(ProgressRange.SixMonths, 2026, 4, 19)]
    [InlineData(ProgressRange.TwelveMonths, 2025, 10, 19)]
    public void StartOf_MonthRange_CountsBackFromToday(ProgressRange range, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), ProgressRanges.StartOf(range, Today));
    }

    [Fact]
    public void StartOf_EndOfALongerMonth_ClampsToTheShorterMonth()
    {
        Assert.Equal(new DateOnly(2026, 2, 28), ProgressRanges.StartOf(ProgressRange.ThreeMonths, new DateOnly(2026, 5, 31)));
    }

    [Fact]
    public void StartOf_All_HasNoStart()
    {
        Assert.Null(ProgressRanges.StartOf(ProgressRange.All, Today));
    }

    [Fact]
    public void Filter_ThreeMonths_KeepsTheStartDayAndLater()
    {
        ProgressPoint[] points = [Point(1, 2026, 7, 18), Point(2, 2026, 7, 19), Point(3, 2026, 10, 19)];

        var filtered = ProgressRanges.Filter(points, ProgressRange.ThreeMonths, Today);

        Assert.Equal([2, 3], filtered.Select(point => point.SessionId));
    }

    [Fact]
    public void Filter_All_KeepsEveryPoint()
    {
        ProgressPoint[] points = [Point(1, 2020, 1, 1), Point(2, 2026, 10, 19)];

        Assert.Equal(points, ProgressRanges.Filter(points, ProgressRange.All, Today));
    }

    [Fact]
    public void Filter_NothingInRange_IsEmpty()
    {
        Assert.Empty(ProgressRanges.Filter([Point(1, 2025, 1, 1)], ProgressRange.SixMonths, Today));
    }

    private static ProgressPoint Point(int sessionId, int year, int month, int day) => new(sessionId, new DateOnly(year, month, day), sessionId);
}
