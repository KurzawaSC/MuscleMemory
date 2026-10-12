using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Services;
using NSubstitute;

namespace MuscleMemory.Tests.Services;

public sealed class WorkoutSummaryServiceTests
{
    private const int BenchId = 11;
    private const int RowId = 12;
    private const int SquatId = 13;

    private readonly IWorkoutSetRepository _sets = Substitute.For<IWorkoutSetRepository>();
    private readonly WorkoutSummaryService _service;

    public WorkoutSummaryServiceTests() => _service = new WorkoutSummaryService(_sets);

    [Fact]
    public async Task BuildAsync_PerformedExercises_ReadsSetsInOneBatch()
    {
        GivenSets();

        await _service.BuildAsync([Exercise(BenchId, "Bench"), Exercise(RowId, "Row")]);

        await _sets.Received(1).GetForSessionExercisesAsync(Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { BenchId, RowId })));
    }

    [Fact]
    public async Task BuildAsync_LoggedSets_GroupsByExerciseInPerformedOrder()
    {
        GivenSets(Set(RowId, 1, 60, 10), Set(BenchId, 1, 80, 8));

        var summary = await _service.BuildAsync([Exercise(BenchId, "Bench"), Exercise(RowId, "Row")]);

        Assert.Equal(["Bench", "Row"], summary.Exercises.Select(exercise => exercise.ExerciseName));
    }

    [Fact]
    public async Task BuildAsync_SetsOutOfOrder_OrdersSetsByNumber()
    {
        GivenSets(Set(BenchId, 3, 70, 10), Set(BenchId, 1, 80, 8), Set(BenchId, 2, 75, 9));

        var summary = await _service.BuildAsync([Exercise(BenchId, "Bench")]);

        Assert.Equal([1, 2, 3], Assert.Single(summary.Exercises).Sets.Select(set => set.SetNumber));
    }

    [Fact]
    public async Task BuildAsync_ExerciseWithoutSets_LeavesItOut()
    {
        GivenSets(Set(RowId, 1, 60, 10));

        var summary = await _service.BuildAsync([Exercise(BenchId, "Bench"), Exercise(RowId, "Row"), Exercise(SquatId, "Squat")]);

        Assert.Equal("Row", Assert.Single(summary.Exercises).ExerciseName);
    }

    [Fact]
    public async Task BuildAsync_LoggedSets_SumsVolumeOfEveryExercise()
    {
        GivenSets(Set(BenchId, 1, 80, 8), Set(BenchId, 2, 82.5, 6), Set(RowId, 1, 60, 10));

        var summary = await _service.BuildAsync([Exercise(BenchId, "Bench"), Exercise(RowId, "Row")]);

        Assert.Equal(1735, summary.TotalVolume);
    }

    [Fact]
    public async Task BuildAsync_NoLoggedSets_ReturnsEmptySummary()
    {
        GivenSets();

        var summary = await _service.BuildAsync([Exercise(BenchId, "Bench")]);

        Assert.Empty(summary.Exercises);
        Assert.Equal(0, summary.TotalVolume);
        Assert.False(summary.HasLoggedSets);
    }

    private void GivenSets(params WorkoutSet[] sets) =>
        _sets.GetForSessionExercisesAsync(Arg.Any<IReadOnlyCollection<int>>()).Returns(sets.ToList());

    private static SessionExercise Exercise(int id, string name) => new() { Id = id, ExerciseName = name };

    private static WorkoutSet Set(int sessionExerciseId, int setNumber, double weight, int reps) =>
        new() { SessionExerciseId = sessionExerciseId, SetNumber = setNumber, Weight = weight, Reps = reps };
}
