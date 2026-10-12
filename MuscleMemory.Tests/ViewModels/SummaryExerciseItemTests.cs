using Microsoft.Maui.Graphics;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class SummaryExerciseItemTests
{
    private const string ExerciseName = "Bench";

    [Fact]
    public void Create_EnglishUs_FormatsVolumeAndSetTexts()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 100, 10), Set(2, 82.5, 8));

        Assert.Equal(ExerciseName, item.ExerciseName);
        Assert.Equal("1,660 kg", item.VolumeText);
        Assert.Equal(["100 kg × 10", "82.5 kg × 8"], item.Sets.Select(bar => bar.Text));
        Assert.Equal([1, 2], item.Sets.Select(bar => bar.SetNumber));
    }

    [Fact]
    public void Create_Polish_UsesPolishSeparators()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = Create(Set(1, 100, 10), Set(2, 82.5, 8));

        Assert.Equal($"1{Cultures.PolishGroupSeparator}660 kg", item.VolumeText);
        Assert.Equal("82,5 kg × 8", item.Sets[1].Text);
    }

    [Fact]
    public void Create_SeveralSets_ScalesBarsToBestVolume()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 100, 10), Set(2, 50, 10), Set(3, 75, 10));

        Assert.Equal([1, 0.5, 0.75], item.Sets.Select(bar => bar.FillBounds.Width));
        Assert.All(item.Sets, bar => Assert.Equal(new Rect(0, 0, bar.FillBounds.Width, 1), bar.FillBounds));
    }

    [Fact]
    public void Create_SingleBestSet_MarksOnlyThatSet()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 80, 8), Set(2, 85, 8), Set(3, 80, 6));

        Assert.Equal([false, true, false], item.Sets.Select(bar => bar.IsBest));
    }

    [Fact]
    public void Create_TiedBestSets_MarksNoSet()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 80, 8), Set(2, 80, 8));

        Assert.All(item.Sets, bar => Assert.False(bar.IsBest));
    }

    [Fact]
    public void Create_OneSet_MarksNoSet()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 80, 8));

        Assert.False(Assert.Single(item.Sets).IsBest);
        Assert.Equal(1, item.Sets[0].FillBounds.Width);
    }

    [Fact]
    public void Create_OnlyZeroVolumeSets_DrawsEmptyBars()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create(Set(1, 0, 15), Set(2, 0, 12));

        Assert.Equal("0 kg", item.VolumeText);
        Assert.All(item.Sets, bar => Assert.Equal(0, bar.FillBounds.Width));
    }

    [Fact]
    public void Create_NoSets_ShowsZeroVolume()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = Create();

        Assert.Equal("0 kg", item.VolumeText);
        Assert.Empty(item.Sets);
    }

    private static SummaryExerciseItem Create(params WorkoutSet[] sets) =>
        SummaryExerciseItem.Create(new CompletedExerciseSummary(ExerciseName, sets));

    private static WorkoutSet Set(int setNumber, double weight, int reps) =>
        new() { SetNumber = setNumber, Weight = weight, Reps = reps };
}
