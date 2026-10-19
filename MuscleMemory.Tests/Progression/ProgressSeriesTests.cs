using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class ProgressSeriesTests
{
    private const double Bodyweight = 0;

    private static readonly DateTime MondayMorningUtc = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Fact]
    public void Build_OneSession_TakesTheBestEstimateTheVolumeSumAndNoReps()
    {
        var progress = ProgressSeries.Build(
            [Total(1, MondayMorningUtc, reps: 5, heaviest: 100, volume: 1000), Total(1, MondayMorningUtc, reps: 3, heaviest: 105, volume: 315)],
            Utc);

        var expectedDate = new DateOnly(2026, 3, 2);
        Assert.Equal([new ProgressPoint(1, expectedDate, 116.66666666666667)], progress.OneRepMax);
        Assert.Equal([new ProgressPoint(1, expectedDate, 1315)], progress.Volume);
        Assert.Empty(progress.Reps);
    }

    [Fact]
    public void Build_SessionsOutOfOrder_OrdersThemByStartThenId()
    {
        var progress = ProgressSeries.Build(
            [
                Total(7, MondayMorningUtc.AddDays(2), reps: 5, heaviest: 90, volume: 450),
                Total(9, MondayMorningUtc, reps: 5, heaviest: 80, volume: 400),
                Total(4, MondayMorningUtc, reps: 5, heaviest: 85, volume: 425)
            ],
            Utc);

        Assert.Equal([4, 9, 7], progress.Volume.Select(point => point.SessionId));
    }

    [Fact]
    public void Build_OnlyRepsAboveTheEstimateLimit_SkipsTheSessionForOneRepMaxOnly()
    {
        var progress = ProgressSeries.Build(
            [
                Total(1, MondayMorningUtc, reps: 15, heaviest: 40, volume: 600),
                Total(2, MondayMorningUtc.AddDays(1), reps: 12, heaviest: 40, volume: 480)
            ],
            Utc);

        Assert.Equal([2], progress.OneRepMax.Select(point => point.SessionId));
        Assert.Equal([1, 2], progress.Volume.Select(point => point.SessionId));
    }

    [Fact]
    public void Build_ZeroRepRows_AreNotEligible()
    {
        var progress = ProgressSeries.Build(
            [Total(1, MondayMorningUtc, reps: 0, heaviest: 200, volume: 0), Total(2, MondayMorningUtc.AddDays(1), reps: 5, heaviest: 60, volume: 300)],
            Utc);

        Assert.Equal([2], progress.OneRepMax.Select(point => point.SessionId));
        Assert.Equal([2], progress.Volume.Select(point => point.SessionId));
    }

    [Fact]
    public void Build_AllBodyweight_HasOnlyTheMostRepsSeries()
    {
        var progress = ProgressSeries.Build(
            [
                Total(1, MondayMorningUtc, reps: 12, heaviest: Bodyweight, volume: 0),
                Total(1, MondayMorningUtc, reps: 15, heaviest: Bodyweight, volume: 0),
                Total(2, MondayMorningUtc.AddDays(3), reps: 10, heaviest: Bodyweight, volume: 0)
            ],
            Utc);

        Assert.Equal([15.0, 10.0], progress.Reps.Select(point => point.Value));
        Assert.Empty(progress.OneRepMax);
        Assert.Empty(progress.Volume);
        Assert.Equal([ProgressMetric.Reps], progress.Metrics);
    }

    [Fact]
    public void Build_BodyweightAndWeightedSessions_HasNoRepsSeriesAndSkipsTheBodyweightSession()
    {
        var progress = ProgressSeries.Build(
            [
                Total(1, MondayMorningUtc, reps: 12, heaviest: Bodyweight, volume: 0),
                Total(2, MondayMorningUtc.AddDays(3), reps: 8, heaviest: 10, volume: 80)
            ],
            Utc);

        Assert.Empty(progress.Reps);
        Assert.Equal([2], progress.OneRepMax.Select(point => point.SessionId));
        Assert.Equal([2], progress.Volume.Select(point => point.SessionId));
        Assert.Equal([ProgressMetric.OneRepMax, ProgressMetric.Volume], progress.Metrics);
    }

    [Fact]
    public void Build_LateEveningUtc_DatesThePointInTheLocalZone()
    {
        var eastOfUtc = TimeZoneInfo.CreateCustomTimeZone("UTC+02", TimeSpan.FromHours(2), "UTC+02", "UTC+02");
        var lateEveningUtc = new DateTime(2026, 3, 2, 23, 0, 0, DateTimeKind.Utc);

        var progress = ProgressSeries.Build([Total(1, lateEveningUtc, reps: 5, heaviest: 100, volume: 500)], eastOfUtc);

        Assert.Equal(new DateOnly(2026, 3, 3), Assert.Single(progress.Volume).Date);
    }

    [Fact]
    public void Build_NoTotals_HasNoMetrics()
    {
        var progress = ProgressSeries.Build([], Utc);

        Assert.False(progress.HasAny);
        Assert.Empty(progress.Metrics);
    }

    private static SessionRepTotals Total(int sessionId, DateTime dateUtc, int reps, double heaviest, double volume) => new()
    {
        SessionId = sessionId,
        DateUtc = dateUtc,
        Reps = reps,
        HeaviestWeight = heaviest,
        Volume = volume
    };
}
