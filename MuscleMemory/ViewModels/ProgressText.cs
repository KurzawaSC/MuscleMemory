using System.Globalization;
using MuscleMemory.Charting;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Localization;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public static class ProgressText
{
    public static string Value(ProgressMetric metric, double value) => metric switch
    {
        ProgressMetric.OneRepMax => Format(AppResources.OneRepMaxFormat, value),
        ProgressMetric.Volume => Format(AppResources.VolumeFormat, value),
        ProgressMetric.Reps => Plurals.Format(nameof(AppResources.RepCount_Other), (int)value),
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null)
    };

    public static string Date(DateOnly date) => date.ToString(AppResources.LongDateFormat, CultureInfo.CurrentCulture);

    public static string Selection(ProgressMetric metric, ProgressPoint point) =>
        Format(AppResources.ChartSelectionFormat, Value(metric, point.Value), Date(point.Date));

    public static string Trend(ProgressMetric metric, ProgressTrend? trend) =>
        trend is null
            ? AppResources.ChartTrendUnavailable
            : Format(AppResources.ChartTrendFormat, Format(RateFormat(metric), SignedRate(trend.PerMonth)));

    public static string Description(ProgressMetric metric, ProgressRange range, IReadOnlyList<ProgressPoint> points, ProgressTrend? trend)
    {
        var first = points[0];
        var last = points[^1];
        object[] values =
        [
            metric.ToDescription(),
            range.ToDescription(),
            Value(metric, first.Value),
            Date(first.Date),
            Value(metric, last.Value),
            Date(last.Date),
            Value(metric, points.Max(point => point.Value))
        ];

        return trend is null
            ? Format(AppResources.ChartDescriptionFormat, values)
            : Format(AppResources.ChartDescriptionWithTrendFormat, [.. values, SpokenTrend(metric, trend)]);
    }

    public static string AxisValue(double value, int decimals)
    {
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.NumberDecimalDigits = decimals;
        return value.ToString(DisplayFormats.GroupedNumber, format);
    }

    public static string AxisDate(DateOnly date, DateTickUnit unit) => date.ToString(
        unit switch
        {
            DateTickUnit.Day => AppResources.ChartDayFormat,
            DateTickUnit.Month => AppResources.ChartMonthFormat,
            DateTickUnit.Year => AppResources.ChartYearFormat,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        },
        CultureInfo.CurrentCulture);

    private static string SpokenTrend(ProgressMetric metric, ProgressTrend trend)
    {
        var rate = SignedRate(trend.PerMonth);

        if (rate == SignedRate(0))
        {
            return AppResources.ChartTrendFlatSpoken;
        }

        return Format(metric == ProgressMetric.Reps ? AppResources.ChartTrendRepsSpokenFormat : AppResources.ChartTrendKilogramsSpokenFormat, rate);
    }

    private static string RateFormat(ProgressMetric metric) =>
        metric == ProgressMetric.Reps ? AppResources.ChartRateRepsFormat : AppResources.ChartRateKilogramsFormat;

    private static string SignedRate(double perMonth) => perMonth.ToString(DisplayFormats.SignedRate, CultureInfo.CurrentCulture);

    private static string Format(string format, params object[] values) => string.Format(CultureInfo.CurrentCulture, format, values);
}
