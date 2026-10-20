using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class PersonalRecordItemTests
{
    private static readonly DateTime LocalDate = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Local);

    [Fact]
    public void Placeholder_Kind_ShowsDashesUnderTheRecordLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.Placeholder(RecordKind.MostReps);

        Assert.Equal(("—", "Most reps in a set\n—", "Most reps in a set"), (item.ValueText, item.Caption, item.Description));
    }

    [Fact]
    public void ForHeaviestWeight_EnglishUs_ShowsWeightTimesRepsAndTheDate()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.ForHeaviestWeight(Set(100, 5));

        Assert.Equal("100 kg × 5", item.ValueText);
        Assert.Equal("Heaviest weight\nMar 05, 2026", item.Caption);
        Assert.Equal("Heaviest weight: 100 kg for 5 reps, achieved on Mar 05, 2026.", item.Description);
    }

    [Theory]
    [InlineData(1, "Największy ciężar: 82,5 kg na 1 powtórzenie, osiągnięty 5 mar 2026.")]
    [InlineData(3, "Największy ciężar: 82,5 kg na 3 powtórzenia, osiągnięty 5 mar 2026.")]
    [InlineData(5, "Największy ciężar: 82,5 kg na 5 powtórzeń, osiągnięty 5 mar 2026.")]
    public void ForHeaviestWeight_Polish_UsesPolishFormatsAndPlurals(int reps, string expectedDescription)
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = PersonalRecordItem.ForHeaviestWeight(Set(82.5, reps));

        Assert.Equal($"82,5 kg × {reps}", item.ValueText);
        Assert.Equal("Największy ciężar\n5 mar 2026", item.Caption);
        Assert.Equal(expectedDescription, item.Description);
    }

    [Theory]
    [InlineData(72.91666666666667, "72.9 kg")]
    [InlineData(106.66666666666667, "106.7 kg")]
    [InlineData(119.58333333333333, "119.6 kg")]
    [InlineData(87.5, "87.5 kg")]
    [InlineData(100, "100 kg")]
    [InlineData(1234.56, "1,234.6 kg")]
    public void ForBestOneRepMax_EnglishUs_RoundsToOneDecimal(double estimate, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.ForBestOneRepMax(new OneRepMaxRecord(estimate, Set(80, 5)));

        Assert.Equal(expected, item.ValueText);
    }

    [Theory]
    [InlineData(72.91666666666667, "72,9 kg")]
    [InlineData(106.66666666666667, "106,7 kg")]
    [InlineData(100, "100 kg")]
    [InlineData(1234.56, "1" + Cultures.PolishGroupSeparator + "234,6 kg")]
    public void ForBestOneRepMax_Polish_RoundsToOneDecimalWithAComma(double estimate, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = PersonalRecordItem.ForBestOneRepMax(new OneRepMaxRecord(estimate, Set(80, 5)));

        Assert.Equal(expected, item.ValueText);
    }

    [Fact]
    public void ForBestOneRepMax_EnglishUs_DescribesTheRoundedEstimate()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.ForBestOneRepMax(new OneRepMaxRecord(116.66666666666667, Set(100, 5)));

        Assert.Equal("Estimated 1RM\nMar 05, 2026", item.Caption);
        Assert.Equal("Estimated one-rep max: 116.7 kg, achieved on Mar 05, 2026.", item.Description);
    }

    [Fact]
    public void ForBestSessionVolume_EnglishUs_GroupsDigits()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.ForBestSessionVolume(new SessionVolume { SessionId = 1, DateUtc = LocalDate.ToUniversalTime(), Volume = 4200 });

        Assert.Equal("4,200 kg", item.ValueText);
        Assert.Equal("Best session volume\nMar 05, 2026", item.Caption);
        Assert.Equal("Best session volume: 4,200 kg, achieved on Mar 05, 2026.", item.Description);
    }

    [Fact]
    public void ForMostReps_EnglishUs_ShowsTheRepCount()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = PersonalRecordItem.ForMostReps(Set(0, 15));

        Assert.Equal("15", item.ValueText);
        Assert.Equal("Most reps in a set\nMar 05, 2026", item.Caption);
        Assert.Equal("Most reps in a set: 15 reps, achieved on Mar 05, 2026.", item.Description);
    }

    [Fact]
    public void ForMostReps_Polish_UsesPolishPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = PersonalRecordItem.ForMostReps(Set(0, 22));

        Assert.Equal("Najwięcej powtórzeń w serii\n5 mar 2026", item.Caption);
        Assert.Equal("Rekord powtórzeń w serii: 22 powtórzenia, osiągnięty 5 mar 2026.", item.Description);
    }

    private static PerformedSet Set(double weight, int reps) => new()
    {
        SetId = 1,
        SessionId = 1,
        ExerciseId = 1,
        DateUtc = LocalDate.ToUniversalTime(),
        Weight = weight,
        Reps = reps
    };
}
