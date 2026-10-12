using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class ActiveWorkoutSessionServiceTests : IAsyncLifetime
{
    private const int MissingId = 999;
    private const double HalfKilogramWeight = 82.5;

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _push = await _app.Data.WorkoutAsync("Push Day", _bench, _press);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task StartAsync_Workout_OpensSessionWithTheTemplateSnapshot()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);

        Assert.Null((await _app.Sessions.GetAsync(started.SessionId))?.EndTimeUtc);
        Assert.Equal([_bench.Id, _press.Id], started.Exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal(
            started.Exercises.Select(exercise => exercise.Id),
            (await _app.ActiveSessions.GetExercisesAsync(started.SessionId)).Select(exercise => exercise.Id));
    }

    [Fact]
    public async Task StartAsync_WorkoutWithoutExercises_OpensEmptySession()
    {
        var started = await _app.ActiveSessions.StartAsync(await _app.Data.WorkoutAsync("Empty"));

        Assert.Empty(started.Exercises);
        Assert.NotNull(await _app.Sessions.GetAsync(started.SessionId));
    }

    [Fact]
    public async Task FindResumableAsync_NothingSaved_ReturnsNull() =>
        Assert.Null(await _app.ActiveSessions.FindResumableAsync());

    [Fact]
    public async Task FindResumableAsync_SavedState_ReturnsStateAndOpenSession()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.SaveActiveStateAsync(started, exerciseIndex: 1);

        var resumable = await _app.ActiveSessions.FindResumableAsync();

        Assert.NotNull(resumable);
        Assert.Equal((started.SessionId, 1), (resumable.State.SessionId, resumable.State.CurrentExerciseIndex));
        Assert.Equal((started.SessionId, TestApp.StartUtc), (resumable.Session.Id, resumable.Session.StartTimeUtc));
    }

    [Fact]
    public async Task FindResumableAsync_NewServiceOnSameFile_ResumesWithLoggedSets()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var set = await _app.Data.LogSetAsync(started.Exercises[0], HalfKilogramWeight, 5);
        await _app.Data.SaveActiveStateAsync(started, exerciseIndex: 0);
        await (await _app.Database.ConnectionAsync()).CloseAsync();

        var context = new DatabaseContext(_app.Database.FileSystem);
        var reopened = Reopen(context);
        var resumable = await reopened.FindResumableAsync();
        var exercises = await reopened.GetExercisesAsync(started.SessionId);
        var sets = await reopened.GetSetsAsync(exercises[0].Id);
        await (await context.GetConnectionAsync()).CloseAsync();

        Assert.Equal(started.SessionId, resumable?.Session.Id);
        Assert.Equal([_bench.Id, _press.Id], exercises.Select(exercise => exercise.ExerciseId));
        Assert.Equal((set.Id, HalfKilogramWeight, 1), (Assert.Single(sets).Id, sets[0].Weight, sets[0].SetNumber));
    }

    [Fact]
    public async Task FindResumableAsync_SessionFinishedButStateLeft_ClearsTheState()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.SaveActiveStateAsync(started);
        await _app.Sessions.FinishOrDiscardAsync(started.SessionId);

        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task FindResumableAsync_SessionMissing_ClearsTheState()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.SaveActiveStateAsync(started);
        await _app.Sessions.FinishOrDiscardAsync(started.SessionId);

        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task FindResumableAsync_StateWrittenForUnknownSession_ClearsTheState()
    {
        var connection = await _app.Database.ConnectionAsync();
        await connection.InsertOrReplaceAsync(new ActiveWorkoutState { SessionId = MissingId });

        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task FinishAsync_LoggedSets_EndsSessionAndClearsState()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.SaveActiveStateAsync(started);
        _app.Clock.Advance(TestDataBuilder.SessionLength);

        await _app.ActiveSessions.FinishAsync(started.SessionId);

        Assert.Equal(TestApp.StartUtc + TestDataBuilder.SessionLength, (await _app.Sessions.GetAsync(started.SessionId))?.EndTimeUtc);
        Assert.Null(await _app.ActiveState.GetAsync());
        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
    }

    [Fact]
    public async Task FinishAsync_NoSets_DiscardsSessionAndClearsState()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.SaveActiveStateAsync(started);

        await _app.ActiveSessions.FinishAsync(started.SessionId);

        Assert.Null(await _app.Sessions.GetAsync(started.SessionId));
        Assert.Empty(await _app.ActiveSessions.GetExercisesAsync(started.SessionId));
        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task FinishAsync_AllSetsDeleted_DiscardsTheSession()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var set = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.ActiveSessions.DeleteSetAsync(set.Id);

        await _app.ActiveSessions.FinishAsync(started.SessionId);

        Assert.Null(await _app.Sessions.GetAsync(started.SessionId));
    }

    [Fact]
    public async Task SaveStateAsync_AfterFinish_IsIgnored()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.ActiveSessions.FinishAsync(started.SessionId);

        await _app.Data.SaveActiveStateAsync(started);

        Assert.Null(await _app.ActiveSessions.FindResumableAsync());
    }

    [Fact]
    public async Task AddSetAsync_Sets_AreListedInOrder()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.LogSetAsync(started.Exercises[0], HalfKilogramWeight, 4);

        var sets = await _app.ActiveSessions.GetSetsAsync(started.Exercises[0].Id);

        Assert.Equal([(1, 80.0, 5), (2, HalfKilogramWeight, 4)], sets.Select(set => (set.SetNumber, set.Weight, set.Reps)));
    }

    [Fact]
    public async Task UpdateSetAsync_LoggedSet_ChangesItsValues()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var set = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);

        await _app.ActiveSessions.UpdateSetAsync(set.Id, HalfKilogramWeight, 6);

        var stored = Assert.Single(await _app.ActiveSessions.GetSetsAsync(started.Exercises[0].Id));
        Assert.Equal((HalfKilogramWeight, 6), (stored.Weight, stored.Reps));
    }

    [Fact]
    public async Task DeleteSetAsync_FirstSet_RenumbersTheRest()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var first = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.LogSetAsync(started.Exercises[0], HalfKilogramWeight, 4);

        await _app.ActiveSessions.DeleteSetAsync(first.Id);

        var remaining = Assert.Single(await _app.ActiveSessions.GetSetsAsync(started.Exercises[0].Id));
        Assert.Equal((1, HalfKilogramWeight), (remaining.SetNumber, remaining.Weight));
    }

    [Fact]
    public async Task GetLastSessionSetsAsync_PreviousSession_ReturnsItsSets()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        var current = await _app.ActiveSessions.StartAsync(_push);

        var last = await _app.ActiveSessions.GetLastSessionSetsAsync(_bench.Id, current.SessionId);

        Assert.Equal(80.0, Assert.Single(last).Weight);
    }

    [Fact]
    public async Task IsPlanCompleteAsync_EveryPlannedSetLogged_ReturnsTrue()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await LogPlannedSetsAsync(started.Exercises[0]);
        await LogPlannedSetsAsync(started.Exercises[1]);

        Assert.True(await _app.ActiveSessions.IsPlanCompleteAsync(started.Exercises));
    }

    [Fact]
    public async Task IsPlanCompleteAsync_OneSetShort_ReturnsFalse()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await LogPlannedSetsAsync(started.Exercises[0]);
        await LogPlannedSetsAsync(started.Exercises[1], TestDataBuilder.PlannedSets - 1);

        Assert.False(await _app.ActiveSessions.IsPlanCompleteAsync(started.Exercises));
    }

    [Fact]
    public async Task IsPlanCompleteAsync_ExtraSetsLogged_ReturnsTrue()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        await LogPlannedSetsAsync(started.Exercises[0], TestDataBuilder.PlannedSets + 2);
        await LogPlannedSetsAsync(started.Exercises[1]);

        Assert.True(await _app.ActiveSessions.IsPlanCompleteAsync(started.Exercises));
    }

    [Fact]
    public async Task IsPlanCompleteAsync_ExerciseWithoutPlannedSets_IsIgnored()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var unplanned = new SessionExercise { WorkoutSessionId = started.SessionId, ExerciseId = _bench.Id, ExerciseName = _bench.Name };
        await _app.SessionExercises.AppendToSessionAsync(unplanned);
        await LogPlannedSetsAsync(started.Exercises[0]);
        await LogPlannedSetsAsync(started.Exercises[1]);

        Assert.True(await _app.ActiveSessions.IsPlanCompleteAsync([.. started.Exercises, unplanned]));
    }

    [Fact]
    public async Task IsPlanCompleteAsync_NothingPlanned_ReturnsFalse()
    {
        var started = await _app.ActiveSessions.StartAsync(_push);
        var unplanned = new SessionExercise { WorkoutSessionId = started.SessionId, ExerciseId = _bench.Id, ExerciseName = _bench.Name };
        await _app.SessionExercises.AppendToSessionAsync(unplanned);
        await _app.Data.LogSetAsync(unplanned, 80, 5);

        Assert.False(await _app.ActiveSessions.IsPlanCompleteAsync([unplanned]));
    }

    private async Task LogPlannedSetsAsync(SessionExercise performed, int count = TestDataBuilder.PlannedSets)
    {
        for (var set = 0; set < count; set++)
        {
            await _app.Data.LogSetAsync(performed, 80, 5);
        }
    }

    private ActiveWorkoutSessionService Reopen(DatabaseContext context) => new(
        new WorkoutRepository(context),
        new WorkoutSessionRepository(context, _app.Clock),
        new SessionExerciseRepository(context),
        new WorkoutSetRepository(context),
        new ActiveWorkoutStateRepository(context));
}
