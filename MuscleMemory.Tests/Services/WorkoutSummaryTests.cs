using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutSummaryTests
{
    [Fact]
    public void HasLoggedSets_ExerciseWithSets_ReturnsTrue()
    {
        var summary = new WorkoutSummary(0, [new CompletedExerciseSummary("Bench", [new WorkoutSet()])]);

        Assert.True(summary.HasLoggedSets);
    }

    [Fact]
    public void HasLoggedSets_OnlyEmptyExercises_ReturnsFalse()
    {
        var summary = new WorkoutSummary(0, [new CompletedExerciseSummary("Bench", [])]);

        Assert.False(summary.HasLoggedSets);
    }

    [Fact]
    public void HasLoggedSets_NoExercises_ReturnsFalse() =>
        Assert.False(new WorkoutSummary(0, []).HasLoggedSets);
}
