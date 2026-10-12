using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
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

    [Fact]
    public async Task BuildAsync_LoggedSession_SummarisesWhatWasStored()
    {
        await using var app = new TestApp();
        var started = await StartPushAsync(app);
        await app.Data.LogSetAsync(started.Exercises[1], 42.5, 8);
        await app.Data.LogSetAsync(started.Exercises[0], 82.5, 5);
        await app.Data.LogSetAsync(started.Exercises[0], 80, 5);

        var summary = await app.Summary.BuildAsync(await app.ActiveSessions.GetExercisesAsync(started.SessionId));

        Assert.Equal(1152.5, summary.TotalVolume);
        Assert.True(summary.HasLoggedSets);
        Assert.Equal([("Bench", 2), ("Press", 1)], summary.Exercises.Select(exercise => (exercise.ExerciseName, exercise.Sets.Count)));
        Assert.Equal([82.5, 80.0], summary.Exercises[0].Sets.Select(set => set.Weight));
    }

    [Fact]
    public async Task BuildAsync_SessionAfterDeletingASet_LeavesItOut()
    {
        await using var app = new TestApp();
        var started = await StartPushAsync(app);
        var deleted = await app.Data.LogSetAsync(started.Exercises[0], 100, 1);
        await app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await app.ActiveSessions.DeleteSetAsync(deleted.Id);

        var summary = await app.Summary.BuildAsync(started.Exercises);

        Assert.Equal(400, summary.TotalVolume);
        Assert.Equal(1, Assert.Single(summary.Exercises).Sets.Single().SetNumber);
    }

    [Fact]
    public async Task BuildAsync_SessionWithoutSets_IsEmpty()
    {
        await using var app = new TestApp();
        var started = await StartPushAsync(app);

        var summary = await app.Summary.BuildAsync(started.Exercises);

        Assert.Empty(summary.Exercises);
        Assert.Equal(0, summary.TotalVolume);
        Assert.False(summary.HasLoggedSets);
    }

    private static async Task<StartedSession> StartPushAsync(TestApp app)
    {
        var bench = await app.Data.ExerciseAsync("Bench");
        var press = await app.Data.ExerciseAsync("Press");
        return await app.Data.StartAsync(await app.Data.WorkoutAsync("Push", bench, press));
    }

    private void GivenSets(params WorkoutSet[] sets) =>
        _sets.GetForSessionExercisesAsync(Arg.Any<IReadOnlyCollection<int>>()).Returns(sets.ToList());

    private static SessionExercise Exercise(int id, string name) => new() { Id = id, ExerciseName = name };

    private static WorkoutSet Set(int sessionExerciseId, int setNumber, double weight, int reps) =>
        new() { SessionExerciseId = sessionExerciseId, SetNumber = setNumber, Weight = weight, Reps = reps };
}
