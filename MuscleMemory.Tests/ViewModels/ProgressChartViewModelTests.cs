using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ProgressChartViewModelTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(NowUtc.UtcDateTime);

    private static readonly ExerciseProgress Weighted = new(
        [Point(1, 200, 80), Point(2, 140, 90), Point(3, 80, 95), Point(4, 5, 100)],
        [Point(1, 200, 2000), Point(2, 140, 2100), Point(3, 80, 2300), Point(4, 5, 2600)],
        []);

    private readonly ProgressChartViewModel _chart = new(new TestClock(NowUtc));

    [Fact]
    public void New_BeforeTheProgressArrives_ShowsNeitherChartNorTooShort()
    {
        _chart.Prepare([ProgressMetric.OneRepMax, ProgressMetric.Volume]);

        Assert.Equal((false, false, false), (_chart.IsLoaded, _chart.HasChart, _chart.IsTooShort));
        Assert.Equal([ProgressMetric.OneRepMax, ProgressMetric.Volume], _chart.Metrics.Select(option => (ProgressMetric)option.Value));
        Assert.Equal(ProgressMetric.OneRepMax, _chart.SelectedMetric);
    }

    [Fact]
    public void Show_PreparedMetrics_KeepsTheSameOptions()
    {
        _chart.Prepare([ProgressMetric.OneRepMax, ProgressMetric.Volume]);
        var prepared = _chart.Metrics.ToList();

        _chart.Show(Weighted);

        Assert.Equal(prepared, _chart.Metrics);
        Assert.True(prepared.Zip(_chart.Metrics).All(pair => ReferenceEquals(pair.First, pair.Second)));
    }

    [Fact]
    public void Show_WeightedProgress_ChartsEveryOneRepMaxPointByDefault()
    {
        _chart.Show(Weighted);

        Assert.Equal(ProgressRange.All, _chart.SelectedRange);
        Assert.Equal([80.0, 90, 95, 100], _chart.Chart?.Points.Select(point => point.Y));
        Assert.Equal(Today.AddDays(-200).DayNumber, _chart.Chart?.FirstX);
        Assert.Equal(Today.AddDays(-5).DayNumber, _chart.Chart?.LastX);
    }

    [Theory]
    [InlineData(ProgressRange.ThreeMonths, 2)]
    [InlineData(ProgressRange.SixMonths, 3)]
    [InlineData(ProgressRange.TwelveMonths, 4)]
    [InlineData(ProgressRange.All, 4)]
    public void SelectedRange_Changed_ChartsOnlyThePointsInIt(ProgressRange range, int expectedPoints)
    {
        _chart.Show(Weighted);

        _chart.SelectedRange = range;

        Assert.Equal(expectedPoints, _chart.Chart?.Points.Count);
    }

    [Fact]
    public void SelectedMetric_Volume_ChartsTheVolumeSeries()
    {
        _chart.Show(Weighted);
        var changes = PropertyChanges.Record(_chart);

        _chart.SelectedMetric = ProgressMetric.Volume;

        Assert.Equal([2000.0, 2100, 2300, 2600], _chart.Chart?.Points.Select(point => point.Y));
        Assert.Contains(nameof(ProgressChartViewModel.Chart), changes);
        Assert.Contains(nameof(ProgressChartViewModel.Description), changes);
    }

    [Fact]
    public void Show_BodyweightProgress_OffersOnlyReps()
    {
        _chart.Prepare([ProgressMetric.OneRepMax]);

        _chart.Show(new ExerciseProgress([], [], [Point(1, 30, 12), Point(2, 10, 15)]));

        Assert.Equal([ProgressMetric.Reps], _chart.Metrics.Select(option => (ProgressMetric)option.Value));
        Assert.Equal(ProgressMetric.Reps, _chart.SelectedMetric);
        Assert.Equal([12.0, 15], _chart.Chart?.Points.Select(point => point.Y));
    }

    [Fact]
    public void Show_OnePointInRange_IsTooShortWithoutAChartOrDescription()
    {
        _chart.Show(new ExerciseProgress([Point(1, 5, 100)], [Point(1, 5, 500)], []));

        Assert.Equal((true, false, true), (_chart.IsLoaded, _chart.HasChart, _chart.IsTooShort));
        Assert.Null(_chart.Chart);
        Assert.Equal(string.Empty, _chart.Description);
    }

    [Fact]
    public void SelectedRange_NarrowedToOnePoint_BecomesTooShort()
    {
        _chart.Show(new ExerciseProgress([Point(1, 300, 80), Point(2, 5, 100)], [], []));

        _chart.SelectedRange = ProgressRange.ThreeMonths;

        Assert.True(_chart.IsTooShort);
        _chart.SelectedRange = ProgressRange.All;
        Assert.True(_chart.HasChart);
    }

    [Fact]
    public void SelectPoint_Index_HighlightsItAndCaptionsItsValueAndDate()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _chart.Show(Weighted);

        _chart.SelectPointCommand.Execute(1);

        Assert.Equal(1, _chart.SelectedIndex);
        Assert.Equal($"90 kg · {Today.AddDays(-140):MMM dd, yyyy}", _chart.SelectionText);
    }

    [Fact]
    public void SelectPoint_Nothing_ClearsTheSelectionAndShowsTheHint()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _chart.Show(Weighted);
        _chart.SelectPointCommand.Execute(2);

        _chart.SelectPointCommand.Execute(null);

        Assert.Equal(ProgressChartViewModel.NoSelection, _chart.SelectedIndex);
        Assert.Equal("Tap a point to see its value", _chart.SelectionText);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void SelectPoint_OutsideTheRange_SelectsNothing(int index)
    {
        _chart.Show(Weighted);

        _chart.SelectPointCommand.Execute(index);

        Assert.Equal(ProgressChartViewModel.NoSelection, _chart.SelectedIndex);
    }

    [Fact]
    public void SelectPoint_TooShort_SelectsNothing()
    {
        _chart.Show(new ExerciseProgress([Point(1, 5, 100)], [], []));

        _chart.SelectPointCommand.Execute(0);

        Assert.Equal(ProgressChartViewModel.NoSelection, _chart.SelectedIndex);
    }

    [Fact]
    public void SelectedRange_Changed_ClearsTheSelection()
    {
        _chart.Show(Weighted);
        _chart.SelectPointCommand.Execute(3);

        _chart.SelectedRange = ProgressRange.SixMonths;

        Assert.Equal(ProgressChartViewModel.NoSelection, _chart.SelectedIndex);
    }

    [Fact]
    public void Show_ThreePointsOverFourteenDays_ShowsAndDrawsTheTrend()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _chart.Show(new ExerciseProgress([Point(1, 32, 100), Point(2, 16, 104), Point(3, 0, 108)], [], []));

        Assert.Equal("Trend +7.5 kg / month", _chart.TrendText);
        Assert.Equal((Today.AddDays(-32).DayNumber, 100.0), (_chart.Chart?.TrendStart?.X, _chart.Chart?.TrendStart?.Y));
        Assert.Equal((Today.DayNumber, 108.0), (_chart.Chart?.TrendEnd?.X, _chart.Chart?.TrendEnd?.Y));
    }

    [Fact]
    public void SelectedRange_TwoPointsLeft_HidesTheTrend()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _chart.Show(Weighted);
        Assert.StartsWith("Trend +", _chart.TrendText);

        _chart.SelectedRange = ProgressRange.ThreeMonths;

        Assert.Equal("Not enough sessions for a trend", _chart.TrendText);
        Assert.Null(_chart.Chart?.TrendStart);
        Assert.Null(_chart.Chart?.TrendEnd);
    }

    [Fact]
    public void Show_WeightedProgress_LabelsTheAxesInTheCurrentCulture()
    {
        using var scope = new CultureScope(Cultures.Polish);

        _chart.Show(Weighted);
        _chart.SelectedMetric = ProgressMetric.Volume;

        Assert.Equal(
            [$"1{Cultures.PolishGroupSeparator}750", $"2{Cultures.PolishGroupSeparator}000", $"2{Cultures.PolishGroupSeparator}250", $"2{Cultures.PolishGroupSeparator}500", $"2{Cultures.PolishGroupSeparator}750"],
            _chart.Chart?.YTicks.Select(tick => tick.Label));
        Assert.Equal(["maj 2026", "lip 2026", "wrz 2026"], _chart.Chart?.XTicks.Select(tick => tick.Label));
    }

    [Fact]
    public void Description_OneRepMaxWithTrend_SummarisesTheRangeForScreenReaders()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _chart.Show(new ExerciseProgress([Point(1, 32, 100), Point(2, 16, 110), Point(3, 0, 108)], [], []));

        Assert.Equal(
            $"Estimated one-rep max, all time: from 100 kg on {Today.AddDays(-32):MMM dd, yyyy} to 108 kg on {Today:MMM dd, yyyy}, best 110 kg, trend +7.5 kg per month.",
            _chart.Description);
    }

    [Fact]
    public void Ranges_EnglishUs_AreShortLabelsWithSpokenDescriptions()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var chart = new ProgressChartViewModel(new TestClock(NowUtc));

        Assert.Equal(["3M", "6M", "1Y", "All"], chart.Ranges.Select(option => option.Label));
        Assert.Equal(["last 3 months", "last 6 months", "last 12 months", "all time"], chart.Ranges.Select(option => option.Description));
    }

    private static ProgressPoint Point(int sessionId, int daysAgo, double value) => new(sessionId, Today.AddDays(-daysAgo), value);
}
