using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class SuggestionItemTests
{
    private static SuggestionItem Create(SuggestionOutcome outcome, double weight, params int[] targets) =>
        SuggestionItem.Create(new ProgressionSuggestion(outcome, weight, targets));

    [Fact]
    public void Create_IncreaseInEnglish_ShowsTheNewWeightAndOneTarget()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(SuggestionOutcome.Increase, 82.5, 8, 8, 8);

        Assert.Equal("Increase: 82.5 kg × 8", item.Text);
        Assert.Equal("Suggestion: increase the weight to 82.5 kg, 3 sets of 8 reps.", item.Description);
    }

    [Fact]
    public void Create_IncreaseInPolish_UsesTheDecimalCommaAndPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = Create(SuggestionOutcome.Increase, 82.5, 8, 8, 8);

        Assert.Equal("Zwiększ: 82,5 kg × 8", item.Text);
        Assert.Equal("Sugestia: zwiększ ciężar do 82,5 kg, 3 serie po 8 powtórzeń.", item.Description);
    }

    [Fact]
    public void Create_HoldWithDifferentTargetsInEnglish_ListsEveryTarget()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(SuggestionOutcome.Hold, 80, 9, 9, 8);

        Assert.Equal("Hold: 80 kg × 9 / 9 / 8", item.Text);
        Assert.Equal("Suggestion: keep 80 kg, sets of 9, 9 and 8 reps.", item.Description);
    }

    [Fact]
    public void Create_HoldWithDifferentTargetsInPolish_ListsEveryTarget()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = Create(SuggestionOutcome.Hold, 80, 9, 9, 8);

        Assert.Equal("Utrzymaj: 80 kg × 9 / 9 / 8", item.Text);
        Assert.Equal("Sugestia: utrzymaj 80 kg, serie po 9, 9 i 8 powtórzeń.", item.Description);
    }

    [Fact]
    public void Create_HoldWithEqualTargets_ShowsOneTarget()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(SuggestionOutcome.Hold, 80, 10, 10, 10);

        Assert.Equal("Hold: 80 kg × 10", item.Text);
        Assert.Equal("Suggestion: keep 80 kg, 3 sets of 10 reps.", item.Description);
    }

    [Fact]
    public void Create_HoldWithTwoTargets_JoinsThemWithAnd()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("Suggestion: keep 80 kg, sets of 12 and 8 reps.", Create(SuggestionOutcome.Hold, 80, 12, 8).Description);
    }

    [Fact]
    public void Create_DecreaseInEnglish_ShowsTheLowerWeight()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(SuggestionOutcome.Decrease, 77.5, 8, 8, 8);

        Assert.Equal("Decrease: 77.5 kg × 8", item.Text);
        Assert.Equal("Suggestion: decrease the weight to 77.5 kg, 3 sets of 8 reps.", item.Description);
    }

    [Fact]
    public void Create_DecreaseInPolish_ShowsTheLowerWeight()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = Create(SuggestionOutcome.Decrease, 77.5, 8, 8, 8);

        Assert.Equal("Zmniejsz: 77,5 kg × 8", item.Text);
        Assert.Equal("Sugestia: zmniejsz ciężar do 77,5 kg, 3 serie po 8 powtórzeń.", item.Description);
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, "Increase: 83.75 kg × 8")]
    [InlineData(Cultures.Polish, "Zwiększ: 83,75 kg × 8")]
    public void Create_QuarterKilogramWeight_KeepsBothDecimals(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        Assert.Equal(expected, Create(SuggestionOutcome.Increase, 83.75, 8, 8, 8).Text);
    }

    [Fact]
    public void Create_OneSetInPolish_UsesTheSingularSet()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Sugestia: zwiększ ciężar do 82,5 kg, 1 seria po 8 powtórzeń.", Create(SuggestionOutcome.Increase, 82.5, 8).Description);
    }

    [Fact]
    public void Create_FiveSetsOfTwoRepsInPolish_UsesTheMatchingPluralForms()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Sugestia: zwiększ ciężar do 82,5 kg, 5 serii po 2 powtórzenia.", Create(SuggestionOutcome.Increase, 82.5, 2, 2, 2, 2, 2).Description);
    }

    [Fact]
    public void Create_Bodyweight_ShowsZeroKilograms()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("Hold: 0 kg × 16 / 14 / 13", Create(SuggestionOutcome.Hold, 0, 16, 14, 13).Text);
    }
}
