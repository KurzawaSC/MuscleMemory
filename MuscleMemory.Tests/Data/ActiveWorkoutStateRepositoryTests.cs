using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Data;

public sealed class ActiveWorkoutStateRepositoryTests : IAsyncLifetime
{
    private const int MissingId = 999;
    private const int RestSeconds = 90;
    private const int QueuedSaves = 5;

    private readonly TestApp _app = new();
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        var bench = await _app.Data.ExerciseAsync("Bench Press");
        var press = await _app.Data.ExerciseAsync("Overhead Press");
        _push = await _app.Data.WorkoutAsync("Push Day", bench, press);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetAsync_NothingSaved_ReturnsNull() =>
        Assert.Null(await _app.ActiveState.GetAsync());

    [Fact]
    public async Task SaveAsync_OpenSession_StoresEveryFieldUnderTheFixedId()
    {
        var started = await _app.Data.StartAsync(_push);
        var breakEndUtc = TestApp.StartUtc.AddSeconds(RestSeconds);

        await _app.ActiveState.SaveAsync(new ActiveWorkoutState
        {
            SessionId = started.SessionId,
            StartTimeUtc = TestApp.StartUtc,
            CurrentExerciseIndex = 1,
            IsResting = true,
            BreakEndTimeUtc = breakEndUtc,
            RestDurationSeconds = RestSeconds
        });

        var state = await _app.ActiveState.GetAsync();
        Assert.NotNull(state);
        Assert.Equal(
            (DatabaseNames.ActiveWorkoutStateId, started.SessionId, TestApp.StartUtc, 1, true, breakEndUtc, RestSeconds),
            (state.Id, state.SessionId, state.StartTimeUtc, state.CurrentExerciseIndex, state.IsResting, state.BreakEndTimeUtc, state.RestDurationSeconds));
        Assert.Equal((DateTimeKind.Utc, DateTimeKind.Utc), (state.StartTimeUtc.Kind, state.BreakEndTimeUtc.Kind));
    }

    [Fact]
    public async Task SaveAsync_SavedTwice_KeepsOneRowWithTheLatestState()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.SaveActiveStateAsync(started, exerciseIndex: 0);

        await _app.Data.SaveActiveStateAsync(started, exerciseIndex: 1);

        Assert.Equal(1, await _app.Database.CountAsync(nameof(ActiveWorkoutState)));
        Assert.Equal(1, (await _app.ActiveState.GetAsync())?.CurrentExerciseIndex);
    }

    [Fact]
    public async Task SaveAsync_QueuedWithoutAwaiting_KeepsTheLastState()
    {
        var started = await _app.Data.StartAsync(_push);

        await Task.WhenAll(Enumerable.Range(0, QueuedSaves).Select(index =>
            _app.ActiveState.SaveAsync(new ActiveWorkoutState { SessionId = started.SessionId, CurrentExerciseIndex = index })));

        Assert.Equal(QueuedSaves - 1, (await _app.ActiveState.GetAsync())?.CurrentExerciseIndex);
    }

    [Fact]
    public async Task SaveAsync_FinishedSession_WritesNothing()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Sessions.FinishOrDiscardAsync(started.SessionId);

        await _app.Data.SaveActiveStateAsync(started);

        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task SaveAsync_MissingSession_WritesNothing()
    {
        await _app.ActiveState.SaveAsync(new ActiveWorkoutState { SessionId = MissingId });

        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task ClearAsync_QueuedAfterSave_RemovesTheState()
    {
        var started = await _app.Data.StartAsync(_push);

        var save = _app.Data.SaveActiveStateAsync(started);
        var clear = _app.ActiveState.ClearAsync();
        await Task.WhenAll(save, clear);

        Assert.Null(await _app.ActiveState.GetAsync());
    }

    [Fact]
    public async Task Clear_InTransaction_RemovesTheState()
    {
        await _app.Data.SaveActiveStateAsync(await _app.Data.StartAsync(_push));

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.ActiveState.Clear);

        Assert.Null(await _app.ActiveState.GetAsync());
    }
}
