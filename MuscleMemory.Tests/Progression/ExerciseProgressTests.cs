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

    [Fact]
    public void MetricsFor_WeightedRecordKinds_AreOneRepMaxAndVolume()
    {
        Assert.Equal(
            [ProgressMetric.OneRepMax, ProgressMetric.Volume],
            ExerciseProgress.MetricsFor([RecordKind.HeaviestWeight, RecordKind.BestOneRepMax, RecordKind.BestSessionVolume]));
    }

    [Fact]
    public void MetricsFor_NoEstimateRecord_IsVolumeOnly()
    {
        Assert.Equal([ProgressMetric.Volume], ExerciseProgress.MetricsFor([RecordKind.HeaviestWeight, RecordKind.BestSessionVolume]));
    }

    [Fact]
    public void MetricsFor_MostReps_IsReps()
    {
        Assert.Equal([ProgressMetric.Reps], ExerciseProgress.MetricsFor([RecordKind.MostReps]));
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
