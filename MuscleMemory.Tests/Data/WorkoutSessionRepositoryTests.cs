using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using SQLite;

namespace MuscleMemory.Tests.Data;

public sealed class WorkoutSessionRepositoryTests : IAsyncLifetime
{
    private const string PushDay = "Push Day";
    private const string LegDay = "Leg Day";
    private const string Renamed = "Renamed";
    private const int MissingId = 999;
    private const int PushExerciseCount = 2;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _push = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task CreateWithSnapshotAsync_Template_OpensSessionAtClockTime()
    {
        var started = await _app.Sessions.CreateWithSnapshotAsync(_push, await _app.Workouts.GetExercisesAsync(_push.Id));

        var session = await _app.Sessions.GetAsync(started.SessionId);
        Assert.NotNull(session);
        Assert.Equal((_push.Id, PushDay, TestApp.StartUtc, (DateTime?)null), (session.WorkoutId, session.WorkoutName, session.StartTimeUtc, session.EndTimeUtc));
        Assert.Equal(DateTimeKind.Utc, session.StartTimeUtc.Kind);
    }

    [Fact]
    public async Task CreateWithSnapshotAsync_Template_CopiesThePlanInOrder()
    {
        var started = await _app.Sessions.CreateWithSnapshotAsync(_push, await _app.Workouts.GetExercisesAsync(_push.Id));

        var snapshot = await _app.SessionExercises.GetForSessionAsync(started.SessionId);
        Assert.Equal(started.Exercises.Select(exercise => exercise.Id), snapshot.Select(exercise => exercise.Id));
        Assert.Equal(
            [(_bench.Id, _bench.Name, 0), (_press.Id, _press.Name, 1)],
            snapshot.Select(exercise => (exercise.ExerciseId, exercise.ExerciseName, exercise.Order)));
        Assert.All(snapshot, exercise => Assert.Equal(
            (TestDataBuilder.PlannedSets, TestDataBuilder.PlannedReps, TestDataBuilder.BreakSeconds, TestDataBuilder.TargetRpe),
            (exercise.PlannedSets, exercise.PlannedReps, exercise.BreakTimeInSeconds, exercise.TargetRPE)));
    }

    [Fact]
    public async Task CreateWithSnapshotAsync_SnapshotInsertFails_LeavesNoSession()
    {
        await _app.Database.FailOnAsync(nameof(SessionExercise), TableOperation.Insert);

        await Assert.ThrowsAsync<SQLiteException>(async () =>
            await _app.Sessions.CreateWithSnapshotAsync(_push, await _app.Workouts.GetExercisesAsync(_push.Id)));

        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSession)));
    }

    [Fact]
    public async Task CreateWithSnapshotAsync_TemplateEditedLater_KeepsTheSnapshot()
    {
        var started = await _app.Data.StartAsync(_push);

        _bench.Name = Renamed;
        await _app.Catalog.UpdateAsync(_bench);
        await _app.Workouts.UpdateWithExercisesAsync(_push, [TestDataBuilder.Plan(_press)]);

        Assert.Equal(
            ["Bench Press", "Overhead Press"],
            (await _app.SessionExercises.GetForSessionAsync(started.SessionId)).Select(exercise => exercise.ExerciseName));
    }

    [Fact]
    public async Task FinishOrDiscardAsync_LoggedSets_EndsSessionAtClockTime()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        _app.Clock.Advance(TestDataBuilder.SessionLength);

        await _app.Sessions.FinishOrDiscardAsync(started.SessionId);

        var session = await _app.Sessions.GetAsync(started.SessionId);
        Assert.Equal(TestApp.StartUtc + TestDataBuilder.SessionLength, session?.EndTimeUtc);
        Assert.Equal(DateTimeKind.Utc, session?.EndTimeUtc?.Kind);
    }

    [Fact]
    public async Task FinishOrDiscardAsync_NoSets_DeletesSessionAndSnapshot()
    {
        var started = await _app.Data.StartAsync(_push);

        await _app.Sessions.FinishOrDiscardAsync(started.SessionId);

        Assert.Null(await _app.Sessions.GetAsync(started.SessionId));
        Assert.Equal(0, await _app.Database.CountAsync(nameof(SessionExercise)));
    }

    [Fact]
    public async Task FinishOrDiscardAsync_NoSets_KeepsOtherSessions()
    {
        var kept = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        _app.Clock.Advance(OneDay);
        var discarded = await _app.Data.StartAsync(_push);

        await _app.Sessions.FinishOrDiscardAsync(discarded.SessionId);

        Assert.Equal(kept, Assert.Single(await _app.Sessions.GetCompletedForWorkoutAsync(_push.Id)).Id);
        Assert.Equal(PushExerciseCount, await _app.Database.CountAsync(nameof(SessionExercise)));
    }

    [Fact]
    public async Task GetAsync_MissingSession_ReturnsNull() =>
        Assert.Null(await _app.Sessions.GetAsync(MissingId));

    [Fact]
    public async Task GetCompletedByIdsAsync_OpenAndFinished_ReturnsOnlyFinished()
    {
        var finished = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        var open = await _app.Data.StartAsync(_push);

        var sessions = await _app.Sessions.GetCompletedByIdsAsync([finished, open.SessionId, MissingId]);

        Assert.Equal(finished, Assert.Single(sessions).Id);
        Assert.Equal(DateTimeKind.Utc, sessions[0].StartTimeUtc.Kind);
    }

    [Fact]
    public async Task GetCompletedByIdsAsync_NoIds_ReturnsNothing() =>
        Assert.Empty(await _app.Sessions.GetCompletedByIdsAsync([]));

    [Fact]
    public async Task GetCompletedForWorkoutAsync_SeveralSessions_ReturnsThatWorkoutNewestFirst()
    {
        var legs = await _app.Data.WorkoutAsync(LegDay, _press);
        var first = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(legs, TestApp.StartUtc + OneDay, new LoggedSet(0, 40, 8));
        var third = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + (2 * OneDay), new LoggedSet(0, 82.5, 5));
        _app.Clock.Advance(OneDay);
        await _app.Data.StartAsync(_push);

        var sessions = await _app.Sessions.GetCompletedForWorkoutAsync(_push.Id);

        Assert.Equal([third, first], sessions.Select(session => session.Id));
    }

    [Fact]
    public async Task Clear_InTransaction_RemovesEverySession()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.Sessions.Clear);

        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSession)));
    }
}
