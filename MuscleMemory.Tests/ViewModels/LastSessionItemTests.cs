using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class LastSessionItemTests
{
    private static List<WorkoutSet> Sets(params (double Weight, int Reps)[] sets) =>
        [.. sets.Select(set => new WorkoutSet { Weight = set.Weight, Reps = set.Reps })];

    [Fact]
    public void Create_NoSetsInEnglish_SaysItIsTheFirstTime()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(new LastSessionItem("First time", "First time doing this exercise."), LastSessionItem.Create([]));
    }

    [Fact]
    public void Create_NoSetsInPolish_SaysItIsTheFirstTime()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(new LastSessionItem("Pierwszy raz", "Pierwszy raz wykonujesz to ćwiczenie."), LastSessionItem.Create([]));
    }

    [Fact]
    public void Create_SetsInEnglish_ListsThemAndSpeaksEveryOne()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = LastSessionItem.Create(Sets((80, 8), (80, 8), (77.5, 1)));

        Assert.Equal("Last: 80×8, 80×8, 77.5×1", item.Text);
        Assert.Equal("Last time: 80 kg for 8 reps, 80 kg for 8 reps, 77.5 kg for 1 rep.", item.Description);
    }

    [Fact]
    public void Create_SetsInPolish_UsesTheDecimalCommaAndPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = LastSessionItem.Create(Sets((82.5, 5), (82.5, 3), (80, 1)));

        Assert.Equal("Ostatnio: 82,5×5, 82,5×3, 80×1", item.Text);
        Assert.Equal("Ostatnio: 82,5 kg na 5 powtórzeń, 82,5 kg na 3 powtórzenia, 80 kg na 1 powtórzenie.", item.Description);
    }
}
