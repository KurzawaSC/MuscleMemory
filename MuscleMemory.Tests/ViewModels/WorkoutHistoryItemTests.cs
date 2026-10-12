using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class WorkoutHistoryItemTests
{
    private const string DurationText = "47:05";
    private const double SessionVolume = 12345.5;
    private static readonly DateTime LocalSessionStart = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Local);

    [Fact]
    public void Create_EnglishUsAlone_FormatsChipDateStatsAndVolume()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = WorkoutHistoryItem.Create(Session(), DurationText, sharesDate: false);

        Assert.Equal("Mar 05", item.ChipText);
        Assert.Equal("Mar 05, 2026", item.DateText);
        Assert.Equal("Time 47:05 · 3 sets", item.StatsText);
        Assert.Equal("12,345.5", item.VolumeText);
    }

    [Fact]
    public void Create_SharesDateWithAnotherSession_AddsTimeToChip()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = WorkoutHistoryItem.Create(Session(), DurationText, sharesDate: true);

        Assert.Equal("Mar 05 · 09:15", item.ChipText);
    }

    [Fact]
    public void Create_Polish_UsesPolishPatternsPluralsAndSeparators()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var item = WorkoutHistoryItem.Create(Session(), DurationText, sharesDate: true);

        Assert.Equal("5 mar · 09:15", item.ChipText);
        Assert.Equal("5 mar 2026", item.DateText);
        Assert.Equal("Czas 47:05 · 3 serie", item.StatsText);
        Assert.Equal($"12{Cultures.PolishGroupSeparator}345,5", item.VolumeText);
    }

    [Fact]
    public void Create_Exercises_BuildsOneItemPerExerciseWithItsSets()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var session = Session();

        var item = WorkoutHistoryItem.Create(session, DurationText, sharesDate: false);

        Assert.Same(session, item.Session);
        Assert.Equal(session.Exercises, item.Exercises.Select(exercise => exercise.Exercise));
        Assert.Equal([2, 1], item.Exercises.Select(exercise => exercise.Sets.Count));
        Assert.Equal("Set 1, 60 kg, 10 reps", item.Exercises[1].Sets[0].Description);
    }

    private static WorkoutHistorySession Session()
    {
        var startUtc = LocalSessionStart.ToUniversalTime();
        return new WorkoutHistorySession(
            1,
            startUtc,
            startUtc.AddMinutes(47),
            SessionVolume,
            [
                new WorkoutHistoryExercise(1, "Bench", [Set(1, 80, 8), Set(2, 80, 7)]),
                new WorkoutHistoryExercise(2, "Row", [Set(1, 60, 10)])
            ]);
    }

    private static WorkoutSet Set(int setNumber, double weight, int reps) =>
        new() { SetNumber = setNumber, Weight = weight, Reps = reps };
}
