using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class LoggedSetItemTests
{
    [Fact]
    public void Create_EnglishUs_FormatsEveryColumn()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var set = new WorkoutSet { SetNumber = 2, Weight = 82.5, Reps = 8 };

        var item = LoggedSetItem.Create(set);

        Assert.Same(set, item.Set);
        Assert.Equal("2", item.SetNumberText);
        Assert.Equal("82.5 kg", item.WeightText);
        Assert.Equal("8", item.RepsText);
        Assert.Equal("Set 2, 82.5 kg, 8 reps", item.Description);
    }

    [Fact]
    public void Create_EnglishSingleRep_UsesSingular()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = LoggedSetItem.Create(new WorkoutSet { SetNumber = 1, Weight = 100, Reps = 1 });

        Assert.Equal("100 kg", item.WeightText);
        Assert.Equal("Set 1, 100 kg, 1 rep", item.Description);
    }

    [Theory]
    [InlineData(1, "Seria 3, 82,5 kg, 1 powtórzenie")]
    [InlineData(3, "Seria 3, 82,5 kg, 3 powtórzenia")]
    [InlineData(8, "Seria 3, 82,5 kg, 8 powtórzeń")]
    public void Create_Polish_UsesDecimalCommaAndPluralForm(int reps, string expectedDescription)
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = LoggedSetItem.Create(new WorkoutSet { SetNumber = 3, Weight = 82.5, Reps = reps });

        Assert.Equal("82,5 kg", item.WeightText);
        Assert.Equal(expectedDescription, item.Description);
    }

    [Fact]
    public void Create_QuarterKilogram_KeepsFullPrecision()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = LoggedSetItem.Create(new WorkoutSet { SetNumber = 1, Weight = 22.75, Reps = 10 });

        Assert.Equal("22.75 kg", item.WeightText);
    }
}
