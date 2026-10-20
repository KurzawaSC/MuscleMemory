using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Charting;
using MuscleMemory.Extensions;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public partial class ProgressChartViewModel(TimeProvider clock) : ObservableObject
{
    public const int NoSelection = -1;
    public const int MinChartPoints = 2;
    public const ProgressRange DefaultRange = ProgressRange.All;

    private ExerciseProgress? _progress;
    private IReadOnlyList<ProgressPoint> _points = [];

    public ObservableCollection<EnumOption> Metrics { get; } = [];

    public IReadOnlyList<EnumOption> Ranges { get; } = [.. Enum.GetValues<ProgressRange>().Select(EnumOption.For)];

    [ObservableProperty]
    public partial ProgressMetric SelectedMetric { get; set; }

    [ObservableProperty]
    public partial ProgressRange SelectedRange { get; set; } = DefaultRange;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChart))]
    [NotifyPropertyChangedFor(nameof(IsTooShort))]
    public partial ChartModel? Chart { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTooShort))]
    public partial bool IsLoaded { get; private set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; private set; } = NoSelection;

    [ObservableProperty]
    public partial string SelectionText { get; private set; } = AppResources.ChartSelectHint;

    [ObservableProperty]
    public partial string TrendText { get; private set; } = AppResources.ChartTrendUnavailable;

    [ObservableProperty]
    public partial string Description { get; private set; } = string.Empty;

    public bool HasChart => Chart is not null;

    public bool IsTooShort => IsLoaded && Chart is null;

    public void Prepare(IReadOnlyList<ProgressMetric> metrics)
    {
        ShowMetrics(metrics);
        SelectedMetric = metrics.Count > 0 ? metrics[0] : default;
    }

    public void Show(ExerciseProgress progress)
    {
        _progress = progress;
        ShowMetrics(progress.Metrics);

        if (!progress.Metrics.Contains(SelectedMetric) && progress.Metrics.Count > 0)
        {
            SelectedMetric = progress.Metrics[0];
        }

        IsLoaded = true;
        Refresh();
    }

    partial void OnSelectedMetricChanged(ProgressMetric value) => Refresh();

    partial void OnSelectedRangeChanged(ProgressRange value) => Refresh();

    [RelayCommand]
    private void SelectPoint(int? index)
    {
        SelectedIndex = HasChart && index is { } selected && selected >= 0 && selected < _points.Count ? selected : NoSelection;
        SelectionText = SelectedIndex == NoSelection ? AppResources.ChartSelectHint : ProgressText.Selection(SelectedMetric, _points[SelectedIndex]);
    }

    private void ShowMetrics(IReadOnlyList<ProgressMetric> metrics)
    {
        if (!Metrics.Select(option => (ProgressMetric)option.Value).SequenceEqual(metrics))
        {
            Metrics.ReplaceAll(metrics.Select(EnumOption.For));
        }
    }

    private void Refresh()
    {
        if (_progress is null)
        {
            return;
        }

        _points = ProgressRanges.Filter(_progress.For(SelectedMetric), SelectedRange, Today());
        var trend = TrendLine.Find(_points);

        Chart = _points.Count >= MinChartPoints ? BuildChart(_points, trend) : null;
        TrendText = ProgressText.Trend(SelectedMetric, trend);
        Description = HasChart ? ProgressText.Description(SelectedMetric, SelectedRange, _points, trend) : string.Empty;
        SelectPoint(null);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    private static ChartModel BuildChart(IReadOnlyList<ProgressPoint> points, ProgressTrend? trend)
    {
        var first = points.Min(point => point.Date);
        var last = points.Max(point => point.Date);
        var values = NiceScale.For(points.Min(point => point.Value), points.Max(point => point.Value));
        var dates = DateScale.For(first, last);

        return new ChartModel(
            [.. points.Select(point => new ChartPoint(point.Day, point.Value))],
            first.DayNumber,
            last.DayNumber,
            [.. dates.Dates.Select(date => new AxisTick(date.DayNumber, ProgressText.AxisDate(date, dates.Unit)))],
            [.. values.Values.Select(value => new AxisTick(value, ProgressText.AxisValue(value, values.Decimals)))],
            trend is null ? null : new ChartPoint(first.DayNumber, trend.ValueAt(first.DayNumber)),
            trend is null ? null : new ChartPoint(last.DayNumber, trend.ValueAt(last.DayNumber)));
    }
}
