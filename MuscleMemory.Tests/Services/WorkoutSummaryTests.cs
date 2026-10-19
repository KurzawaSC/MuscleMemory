using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutSummaryTests
{
    [Fact]
    public void HasLoggedSets_ExerciseWithSets_ReturnsTrue()
    {
        var summary = new WorkoutSummary(0, [new CompletedExerciseSummary("Bench", [new WorkoutSet()], 0)]);

        Assert.True(summary.HasLoggedSets);
    }

    [Fact]
    public void HasLoggedSets_OnlyEmptyExercises_ReturnsFalse()
    {
        var summary = new WorkoutSummary(0, [new CompletedExerciseSummary("Bench", [], 0)]);

        Assert.False(summary.HasLoggedSets);
    }

    [Fact]
    public void HasLoggedSets_NoExercises_ReturnsFalse() =>
        Assert.False(new WorkoutSummary(0, []).HasLoggedSets);

    [Fact]
    public void NewRecordCount_SeveralExercises_AddsThemUp()
    {
        var summary = new WorkoutSummary(0,
        [
            new CompletedExerciseSummary("Bench", [new WorkoutSet(), new WorkoutSet()], 2),
            new CompletedExerciseSummary("Row", [new WorkoutSet()], 0),
            new CompletedExerciseSummary("Press", [new WorkoutSet()], 1)
        ]);

        Assert.Equal(3, summary.NewRecordCount);
    }
}
