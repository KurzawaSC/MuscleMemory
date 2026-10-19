using MuscleMemory.Progression;

namespace MuscleMemory.Tests.Progression;

public sealed class ExerciseProgressTests
{
    private static readonly ProgressPoint Point = new(1, new DateOnly(2026, 3, 2), 100);

    [Fact]
    public void Metrics_WeightedSeries_ListsOneRepMaxThenVolume()
    {
        Assert.Equal([ProgressMetric.OneRepMax, ProgressMetric.Volume], new ExerciseProgress([Point], [Point], []).Metrics);
    }

    [Fact]
    public void Metrics_OnlyVolume_LeavesOutTheEmptySeries()
    {
        Assert.Equal([ProgressMetric.Volume], new ExerciseProgress([], [Point], []).Metrics);
    }

    [Theory]
    [InlineData(ProgressMetric.OneRepMax)]
    [InlineData(ProgressMetric.Volume)]
    [InlineData(ProgressMetric.Reps)]
    public void For_Metric_ReturnsItsSeries(ProgressMetric metric)
    {
        ExerciseProgress progress = new(
            metric == ProgressMetric.OneRepMax ? [Point] : [],
            metric == ProgressMetric.Volume ? [Point] : [],
            metric == ProgressMetric.Reps ? [Point] : []);

        Assert.Equal([Point], progress.For(metric));
        Assert.True(progress.HasAny);
    }
}
