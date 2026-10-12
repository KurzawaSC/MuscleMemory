using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class WorkoutListItemTests
{
    private readonly Workout _workout = new() { Name = "Push A" };

    [Fact]
    public void Create_ExercisesOutOfOrder_JoinsNamesInPlanOrder()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = WorkoutListItem.Create(_workout, [Exercise("Dips", 2, 3), Exercise("Bench", 0, 4), Exercise("Press", 1, 3)]);

        Assert.Same(_workout, item.Workout);
        Assert.Equal("Bench · Press · Dips", item.ExerciseSummary);
    }

    [Fact]
    public void Create_Exercises_SumsPlannedSets()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = WorkoutListItem.Create(_workout, [Exercise("Bench", 0, 4), Exercise("Press", 1, 3)]);

        Assert.Equal(7, item.TotalSets);
        Assert.True(item.HasExercises);
        Assert.Equal("7 sets", item.SetsText);
    }

    [Fact]
    public void Create_NoExercises_ReportsEmptyPlan()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var item = WorkoutListItem.Create(_workout, []);

        Assert.Equal(string.Empty, item.ExerciseSummary);
        Assert.Equal(0, item.TotalSets);
        Assert.False(item.HasExercises);
        Assert.Equal("0 sets", item.SetsText);
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, 1, "1 set")]
    [InlineData(Cultures.Polish, 1, "1 seria")]
    [InlineData(Cultures.Polish, 4, "4 serie")]
    [InlineData(Cultures.Polish, 12, "12 serii")]
    public void SetsText_Count_UsesPluralForUICulture(string culture, int sets, string expected)
    {
        using var scope = new CultureScope(culture);

        var item = WorkoutListItem.Create(_workout, [Exercise("Bench", 0, sets)]);

        Assert.Equal(expected, item.SetsText);
    }

    private static WorkoutExercise Exercise(string name, int order, int sets) =>
        new() { ExerciseName = name, Order = order, Sets = sets };
}
