using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using SQLite;

namespace MuscleMemory.Tests.Data;

public sealed class SessionExerciseRepositoryTests : IAsyncLifetime
{
    private const int ConcurrentAppends = 6;

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Exercise _squat = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _squat = await _app.Data.ExerciseAsync("Back Squat");
        _push = await _app.Data.WorkoutAsync("Push Day", _bench, _press);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetForSessionAsync_TwoSessions_ReturnsOnlyThatSessionInOrder()
    {
        var first = await _app.Data.StartAsync(_push);
        await _app.Data.StartAsync(_push);

        var exercises = await _app.SessionExercises.GetForSessionAsync(first.SessionId);

        Assert.Equal([_bench.Id, _press.Id], exercises.Select(exercise => exercise.ExerciseId));
        Assert.All(exercises, exercise => Assert.Equal(first.SessionId, exercise.WorkoutSessionId));
    }

    [Fact]
    public async Task GetForSessionsAsync_SeveralSessions_ReturnsEachSessionsExercises()
    {
        var first = await _app.Data.StartAsync(_push);
        var second = await _app.Data.StartAsync(_push);
        await _app.Data.StartAsync(_push);

        var exercises = await _app.SessionExercises.GetForSessionsAsync([first.SessionId, second.SessionId]);

        Assert.Equal(4, exercises.Count);
        Assert.Equal([first.SessionId, second.SessionId], exercises.Select(exercise => exercise.WorkoutSessionId).Distinct().Order());
        Assert.Equal([0, 0, 1, 1], exercises.Select(exercise => exercise.Order));
    }

    [Fact]
    public async Task GetForSessionsAsync_NoIds_ReturnsNothing() =>
        Assert.Empty(await _app.SessionExercises.GetForSessionsAsync([]));

    [Fact]
    public async Task GetForExerciseAsync_SeveralSessions_ReturnsEveryPerformance()
    {
        var first = await _app.Data.StartAsync(_push);
        var second = await _app.Data.StartAsync(_push);

        var performances = await _app.SessionExercises.GetForExerciseAsync(_press.Id);

        Assert.Equal([first.SessionId, second.SessionId], performances.Select(performance => performance.WorkoutSessionId).Order());
        Assert.Empty(await _app.SessionExercises.GetForExerciseAsync(_squat.Id));
    }

    [Fact]
    public async Task AppendToSessionAsync_Session_AddsAfterTheLastExercise()
    {
        var started = await _app.Data.StartAsync(_push);
        var squat = Performance(started, _squat);

        await _app.SessionExercises.AppendToSessionAsync(squat);

        Assert.Equal(2, squat.Order);
        Assert.Equal([_bench.Id, _press.Id, _squat.Id], (await _app.SessionExercises.GetForSessionAsync(started.SessionId)).Select(exercise => exercise.ExerciseId));
    }

    [Fact]
    public async Task AppendToSessionAsync_EmptySession_StartsAtZero()
    {
        var empty = await _app.Data.WorkoutAsync("Empty");
        var started = await _app.Data.StartAsync(empty);
        var squat = Performance(started, _squat);

        await _app.SessionExercises.AppendToSessionAsync(squat);

        Assert.Equal(0, squat.Order);
    }

    [Fact]
    public async Task AppendToSessionAsync_AfterDeletingTheLast_ReusesItsPosition()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.SessionExercises.DeleteAsync(started.Exercises[1].Id);
        var squat = Performance(started, _squat);

        await _app.SessionExercises.AppendToSessionAsync(squat);

        Assert.Equal(1, squat.Order);
    }

    [Fact]
    public async Task AppendToSessionAsync_Concurrent_ClaimsDistinctPositions()
    {
        var started = await _app.Data.StartAsync(_push);
        var appended = Enumerable.Range(0, ConcurrentAppends).Select(_ => Performance(started, _squat)).ToList();

        await Task.WhenAll(appended.Select(exercise => Task.Run(() => _app.SessionExercises.AppendToSessionAsync(exercise))));

        Assert.Equal(Enumerable.Range(2, ConcurrentAppends), appended.Select(exercise => exercise.Order).Order());
    }

    [Fact]
    public async Task DeleteAsync_Performance_RemovesItAndOnlyItsSets()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        var kept = await _app.Data.LogSetAsync(started.Exercises[1], 40, 8);

        await _app.SessionExercises.DeleteAsync(started.Exercises[0].Id);

        Assert.Equal(_press.Id, Assert.Single(await _app.SessionExercises.GetForSessionAsync(started.SessionId)).ExerciseId);
        Assert.Equal(kept.Id, Assert.Single(await _app.Sets.GetForSessionExercisesAsync([started.Exercises[0].Id, started.Exercises[1].Id])).Id);
    }

    [Fact]
    public async Task DeleteAsync_DeleteFails_KeepsPerformanceAndSets()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Database.FailOnAsync(nameof(SessionExercise), TableOperation.Delete);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.SessionExercises.DeleteAsync(started.Exercises[0].Id));

        Assert.Equal(2, (await _app.SessionExercises.GetForSessionAsync(started.SessionId)).Count);
        Assert.Single(await _app.Sets.GetForSessionExerciseAsync(started.Exercises[0].Id));
    }

    [Fact]
    public async Task DeleteAsync_LastExerciseOfFinishedSession_DeletesTheSession()
    {
        var sessionId = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 40, 8));

        foreach (var performed in await _app.SessionExercises.GetForSessionAsync(sessionId))
        {
            await _app.SessionExercises.DeleteAsync(performed.Id);
        }

        Assert.Null(await _app.Sessions.GetAsync(sessionId));
        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task DeleteAsync_OneExerciseLeftInFinishedSession_KeepsTheSession()
    {
        var sessionId = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 40, 8));
        var performed = await _app.SessionExercises.GetForSessionAsync(sessionId);

        await _app.SessionExercises.DeleteAsync(performed[0].Id);

        var session = Assert.Single(await _app.History.GetWorkoutHistoryAsync(_push.Id));
        Assert.Equal((sessionId, _press.Name), (session.SessionId, Assert.Single(session.Exercises).ExerciseName));
    }

    [Fact]
    public async Task DeleteAsync_LastExerciseOfActiveSession_KeepsTheSessionResumable()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.SaveActiveStateAsync(started);

        foreach (var performed in started.Exercises)
        {
            await _app.SessionExercises.DeleteAsync(performed.Id);
        }

        Assert.Null((await _app.Sessions.GetAsync(started.SessionId))?.EndTimeUtc);
        Assert.Equal(started.SessionId, (await _app.ActiveSessions.FindResumableAsync())?.Session.Id);
    }

    [Fact]
    public async Task DeleteAsync_SessionDeleteFails_KeepsTheExerciseAndItsSets()
    {
        var curls = await _app.Data.WorkoutAsync("Curls", _squat);
        var sessionId = await _app.Data.CompletedSessionAsync(curls, TestApp.StartUtc, new LoggedSet(0, 30, 12));
        var performed = Assert.Single(await _app.SessionExercises.GetForSessionAsync(sessionId));
        await _app.Database.FailOnAsync(nameof(WorkoutSession), TableOperation.Delete);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.SessionExercises.DeleteAsync(performed.Id));

        Assert.Single(await _app.SessionExercises.GetForSessionAsync(sessionId));
        Assert.Single(await _app.Sets.GetForSessionExerciseAsync(performed.Id));
    }

    [Fact]
    public async Task Clear_InTransaction_RemovesEveryPerformance()
    {
        await _app.Data.StartAsync(_push);

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.SessionExercises.Clear);

        Assert.Equal(0, await _app.Database.CountAsync(nameof(SessionExercise)));
    }

    private static SessionExercise Performance(StartedSession session, Exercise exercise) => new()
    {
        WorkoutSessionId = session.SessionId,
        ExerciseId = exercise.Id,
        ExerciseName = exercise.Name
    };
}
