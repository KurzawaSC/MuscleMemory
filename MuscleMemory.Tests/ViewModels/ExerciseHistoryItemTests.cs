using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseHistoryItemTests
{
    private static readonly DateTime LocalSessionStart = new(2026, 3, 5, 14, 30, 0, DateTimeKind.Local);

    [Fact]
    public void Create_EnglishUs_FormatsDateSetCountAndVolume()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = ExerciseHistoryItem.Create(Entry(Set(1, 100, 5), Set(2, 102.5, 7), Set(3, 80, 6)));

        Assert.Equal("Thu, Mar 5 · 14:30", item.DateText);
        Assert.Equal("3 sets", item.SetCountText);
        Assert.Equal("1,697.5 kg", item.VolumeText);
    }

    [Fact]
    public void Create_Polish_UsesPolishDateOrderPluralsAndSeparators()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = ExerciseHistoryItem.Create(Entry(Set(1, 100, 5), Set(2, 102.5, 7), Set(3, 80, 6)));

        Assert.Equal("czw., 5 mar · 14:30", item.DateText);
        Assert.Equal("3 serie", item.SetCountText);
        Assert.Equal($"1{Cultures.PolishGroupSeparator}697,5 kg", item.VolumeText);
    }

    [Fact]
    public void Create_Sets_BuildsOneRowPerSetInOrder()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var entry = Entry(Set(1, 100, 5), Set(2, 90, 8));

        var item = ExerciseHistoryItem.Create(entry);

        Assert.Same(entry, item.Entry);
        Assert.Equal(entry.Sets, item.Sets.Select(row => row.Set));
        Assert.Equal(["Set 1, 100 kg, 5 reps", "Set 2, 90 kg, 8 reps"], item.Sets.Select(row => row.Description));
    }

    private static ExerciseHistoryEntry Entry(params WorkoutSet[] sets) =>
        new(LocalSessionStart.ToUniversalTime(), "Push A", sets);

    private static WorkoutSet Set(int setNumber, double weight, int reps) =>
        new() { SetNumber = setNumber, Weight = weight, Reps = reps };
}
