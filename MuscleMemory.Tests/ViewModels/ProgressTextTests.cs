using MuscleMemory.Charting;
using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ProgressTextTests
{
    private static readonly DateOnly April12 = new(2026, 4, 12);
    private static readonly DateOnly October14 = new(2026, 10, 14);

    [Theory]
    [InlineData(ProgressMetric.OneRepMax, 104.58333, "104.6 kg")]
    [InlineData(ProgressMetric.Volume, 12500, "12,500 kg")]
    [InlineData(ProgressMetric.Reps, 1, "1 rep")]
    [InlineData(ProgressMetric.Reps, 30, "30 reps")]
    public void Value_EnglishUs_FormatsByMetric(ProgressMetric metric, double value, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, ProgressText.Value(metric, value));
    }

    [Theory]
    [InlineData(1, "1 powtórzenie")]
    [InlineData(3, "3 powtórzenia")]
    [InlineData(25, "25 powtórzeń")]
    public void Value_PolishReps_UsesPolishPlurals(int reps, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(expected, ProgressText.Value(ProgressMetric.Reps, reps));
    }

    [Fact]
    public void Value_PolishVolume_GroupsWithANoBreakSpace()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal($"12{Cultures.PolishGroupSeparator}500 kg", ProgressText.Value(ProgressMetric.Volume, 12500));
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, "100 kg · Oct 14, 2026")]
    [InlineData(Cultures.Polish, "100 kg · 14 paź 2026")]
    public void Selection_Point_ShowsValueAndDate(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, ProgressText.Selection(ProgressMetric.OneRepMax, new ProgressPoint(1, October14, 100)));
    }

    [Theory]
    [InlineData(ProgressMetric.OneRepMax, 1.2, "Trend +1.2 kg / month")]
    [InlineData(ProgressMetric.Volume, -5, "Trend −5.0 kg / month")]
    [InlineData(ProgressMetric.Reps, 0.5, "Trend +0.5 reps / month")]
    [InlineData(ProgressMetric.OneRepMax, 0.04, "Trend 0 kg / month")]
    [InlineData(ProgressMetric.OneRepMax, -0.04, "Trend 0 kg / month")]
    public void Trend_EnglishUs_ShowsTheSignedRatePerMonthInTheMetricUnit(ProgressMetric metric, double perMonth, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, ProgressText.Trend(metric, PerMonth(perMonth)));
    }

    [Theory]
    [InlineData(ProgressMetric.OneRepMax, 3.44, "Trend +3,4 kg / mies.")]
    [InlineData(ProgressMetric.Reps, -0.5, "Trend −0,5 powt. / mies.")]
    public void Trend_Polish_UsesPolishUnitsAndDecimalComma(ProgressMetric metric, double perMonth, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(expected, ProgressText.Trend(metric, PerMonth(perMonth)));
    }

    [Fact]
    public void Trend_None_SaysThereAreTooFewSessions()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("Not enough sessions for a trend", ProgressText.Trend(ProgressMetric.OneRepMax, null));
    }

    [Fact]
    public void Description_WithTrend_SummarisesTheRange()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var description = ProgressText.Description(ProgressMetric.OneRepMax, ProgressRange.SixMonths, Points(80, 102.5, 100), PerMonth(3.4));

        Assert.Equal(
            "Estimated one-rep max, last 6 months: from 80 kg on Apr 12, 2026 to 100 kg on Oct 14, 2026, best 102.5 kg, trend +3.4 kg per month.",
            description);
    }

    [Fact]
    public void Description_WithoutTrend_LeavesItOut()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var description = ProgressText.Description(ProgressMetric.Volume, ProgressRange.All, Points(2000, 2500), null);

        Assert.Equal("Session volume, all time: from 2,000 kg on Apr 12, 2026 to 2,500 kg on Oct 14, 2026, best 2,500 kg.", description);
    }

    [Fact]
    public void Description_FlatTrend_SaysSoInWords()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var description = ProgressText.Description(ProgressMetric.Reps, ProgressRange.ThreeMonths, Points(12, 15, 12), PerMonth(0.02));

        Assert.Equal("Most reps in a set, last 3 months: from 12 reps on Apr 12, 2026 to 12 reps on Oct 14, 2026, best 15 reps, trend flat.", description);
    }

    [Fact]
    public void Description_Polish_ReadsAsPolish()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var description = ProgressText.Description(ProgressMetric.Reps, ProgressRange.TwelveMonths, Points(12, 15), PerMonth(0.5));

        Assert.Equal(
            "Najwięcej powtórzeń w serii, ostatnie 12 miesięcy: od 12 powtórzeń (12 kwi 2026) do 15 powtórzeń (14 paź 2026), najlepszy wynik 15 powtórzeń, trend +0,5 powtórzenia na miesiąc.",
            description);
    }

    [Theory]
    [InlineData(12500, 0, "12,500")]
    [InlineData(0.25, 2, "0.25")]
    [InlineData(95, 0, "95")]
    [InlineData(1.5, 1, "1.5")]
    public void AxisValue_EnglishUs_HasTheTickDecimals(double value, int decimals, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, ProgressText.AxisValue(value, decimals));
    }

    [Fact]
    public void AxisValue_Polish_GroupsAndUsesTheDecimalComma()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal($"12{Cultures.PolishGroupSeparator}500,5", ProgressText.AxisValue(12500.5, 1));
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, DateTickUnit.Day, "Oct 14")]
    [InlineData(Cultures.EnglishUs, DateTickUnit.Month, "Oct 2026")]
    [InlineData(Cultures.EnglishUs, DateTickUnit.Year, "2026")]
    [InlineData(Cultures.Polish, DateTickUnit.Day, "14 paź")]
    [InlineData(Cultures.Polish, DateTickUnit.Month, "paź 2026")]
    public void AxisDate_Unit_PicksItsGranularity(string culture, DateTickUnit unit, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, ProgressText.AxisDate(October14, unit));
    }

    private static ProgressTrend PerMonth(double perMonth) => new(perMonth / ProgressTrend.DaysPerMonth, 0, 0);

    private static ProgressPoint[] Points(params double[] values) =>
        [.. values.Select((value, index) => new ProgressPoint(index, index == values.Length - 1 ? October14 : April12.AddDays(index), value))];
}
