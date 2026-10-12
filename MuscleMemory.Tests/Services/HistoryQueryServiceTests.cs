using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class HistoryQueryServiceTests : IAsyncLifetime
{
    private const string PushDay = "Push Day";
    private const string UpperDay = "Upper Day";
    private const int MissingId = 999;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
    private static readonly DateTime LocalMidnight = new(2026, 3, 18, 0, 0, 0, DateTimeKind.Local);

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;
    private Workout _upper = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _push = await _app.Data.WorkoutAsync(PushDay, _bench, _press);
        _upper = await _app.Data.WorkoutAsync(UpperDay, _press, _bench);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetExerciseHistoryAsync_NeverLogged_ReturnsNothing() =>
        Assert.Empty(await _app.History.GetExerciseHistoryAsync(_bench.Id));

    [Fact]
    public async Task GetExerciseHistoryAsync_SeveralWorkouts_ReturnsEverySessionNewestFirst()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(_upper, TestApp.StartUtc + OneDay, new LoggedSet(1, 82.5, 5));

        var history = await _app.History.GetExerciseHistoryAsync(_bench.Id);

        Assert.Equal(
            [(TestApp.StartUtc + OneDay, UpperDay, 82.5), (TestApp.StartUtc, PushDay, 80.0)],
            history.Select(entry => (entry.DateUtc, entry.WorkoutName, Assert.Single(entry.Sets).Weight)));
        Assert.All(history, entry => Assert.Equal(DateTimeKind.Utc, entry.DateUtc.Kind));
    }

    [Fact]
    public async Task GetExerciseHistoryAsync_Sets_AreOrderedByNumberWithHalfKilogramVolume()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 82.5, 5), new LoggedSet(0, 77.5, 8));

        var entry = Assert.Single(await _app.History.GetExerciseHistoryAsync(_bench.Id));

        Assert.Equal([1, 2, 3], entry.Sets.Select(set => set.SetNumber));
        Assert.Equal(1432.5, entry.Sets.TotalVolume());
    }

    [Fact]
    public async Task GetExerciseHistoryAsync_OpenSessionAndUnloggedPerformances_AreLeftOut()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(1, 40, 8));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 80, 5));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 85, 3);

        var entry = Assert.Single(await _app.History.GetExerciseHistoryAsync(_bench.Id));

        Assert.Equal(TestApp.StartUtc + OneDay, entry.DateUtc);
    }

    [Fact]
    public async Task GetExerciseHistoryAsync_ExerciseTwiceInOneSession_ReturnsEachPerformance()
    {
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _bench);
        await _app.Data.CompletedSessionAsync(twice, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 60, 12));

        var history = await _app.History.GetExerciseHistoryAsync(_bench.Id);

        Assert.Equal([60.0, 80.0], history.Select(entry => Assert.Single(entry.Sets).Weight).Order());
    }

    [Fact]
    public async Task GetExerciseHistoryAsync_ExerciseDeletedFromCatalog_KeepsItsHistory()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        await _app.Catalog.DeleteAsync(_bench.Id);

        Assert.Single(await _app.History.GetExerciseHistoryAsync(_bench.Id));
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_NoSessions_ReturnsNothing() =>
        Assert.Empty(await _app.History.GetWorkoutHistoryAsync(_push.Id));

    [Fact]
    public async Task GetWorkoutHistoryAsync_SeveralSessions_ReturnsThatWorkoutNewestFirst()
    {
        var first = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(_upper, TestApp.StartUtc + OneDay, new LoggedSet(0, 40, 8));
        var third = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + (2 * OneDay), new LoggedSet(0, 82.5, 5));
        _app.Clock.Advance(OneDay);
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 85, 3);

        var history = await _app.History.GetWorkoutHistoryAsync(_push.Id);

        Assert.Equal([third, first], history.Select(session => session.SessionId));
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_Session_ReportsDurationAndHalfKilogramVolume()
    {
        var duration = new TimeSpan(1, 12, 30);
        await _app.Data.CompletedSessionAsync(
            _push, TestApp.StartUtc, duration, new LoggedSet(0, 82.5, 5), new LoggedSet(0, 80, 5), new LoggedSet(1, 42.5, 8));

        var session = Assert.Single(await _app.History.GetWorkoutHistoryAsync(_push.Id));

        Assert.Equal((TestApp.StartUtc, TestApp.StartUtc + duration, duration), (session.StartTimeUtc, session.EndTimeUtc, session.Duration));
        Assert.Equal(1152.5, session.TotalVolume);
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_Session_GroupsSetsUnderExercisesInPlanOrder()
    {
        await _app.Data.CompletedSessionAsync(
            _push, TestApp.StartUtc, new LoggedSet(1, 40, 8), new LoggedSet(0, 80, 5), new LoggedSet(1, 42.5, 6), new LoggedSet(0, 82.5, 4));

        var session = Assert.Single(await _app.History.GetWorkoutHistoryAsync(_push.Id));

        Assert.Equal([_bench.Name, _press.Name], session.Exercises.Select(exercise => exercise.ExerciseName));
        Assert.Equal([(1, 80.0), (2, 82.5)], session.Exercises[0].Sets.Select(set => (set.SetNumber, set.Weight)));
        Assert.Equal([(1, 40.0), (2, 42.5)], session.Exercises[1].Sets.Select(set => (set.SetNumber, set.Weight)));
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_ExerciseWithoutSets_IsListedEmpty()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        var session = Assert.Single(await _app.History.GetWorkoutHistoryAsync(_push.Id));

        Assert.Equal([1, 0], session.Exercises.Select(exercise => exercise.Sets.Count));
        Assert.Equal(400, session.TotalVolume);
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_SessionsAcrossLocalMidnight_FallOnDifferentLocalDays()
    {
        await _app.Data.CompletedSessionAsync(_push, (LocalMidnight - OneMinute).ToUniversalTime(), OneMinute, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(_push, (LocalMidnight + OneMinute).ToUniversalTime(), OneMinute, new LoggedSet(0, 82.5, 5));

        var history = await _app.History.GetWorkoutHistoryAsync(_push.Id);

        Assert.Equal([LocalMidnight.Date, LocalMidnight.Date.AddDays(-1)], history.Select(session => session.LocalStartTime.Date));
    }

    [Fact]
    public async Task GetWorkoutHistoryAsync_SessionsAtBothEndsOfALocalDay_ShareThatDay()
    {
        await _app.Data.CompletedSessionAsync(_push, LocalMidnight.ToUniversalTime(), OneMinute, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(_push, (LocalMidnight + OneDay - OneMinute).ToUniversalTime(), OneMinute, new LoggedSet(0, 82.5, 5));

        var history = await _app.History.GetWorkoutHistoryAsync(_push.Id);

        Assert.All(history, session => Assert.Equal(LocalMidnight.Date, session.LocalStartTime.Date));
    }

    [Fact]
    public async Task GetExerciseHistoryAsync_SessionJustBeforeLocalMidnight_KeepsItsLocalDay()
    {
        await _app.Data.CompletedSessionAsync(_push, (LocalMidnight - OneMinute).ToUniversalTime(), new LoggedSet(0, 80, 5));

        var entry = Assert.Single(await _app.History.GetExerciseHistoryAsync(_bench.Id));

        Assert.Equal(LocalMidnight - OneMinute, entry.LocalDate);
    }

    [Fact]
    public async Task GetWorkoutHistorySessionAsync_FinishedSession_ReturnsIt()
    {
        var sessionId = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));

        Assert.Equal(sessionId, (await _app.History.GetWorkoutHistorySessionAsync(sessionId))?.SessionId);
    }

    [Fact]
    public async Task GetWorkoutHistorySessionAsync_OpenOrMissingSession_ReturnsNull()
    {
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 80, 5);

        Assert.Null(await _app.History.GetWorkoutHistorySessionAsync(open.SessionId));
        Assert.Null(await _app.History.GetWorkoutHistorySessionAsync(MissingId));
    }

    [Fact]
    public async Task GetWorkoutHistorySessionAsync_AfterRemovingAnExercise_ReflectsTheRemoval()
    {
        var sessionId = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 40, 8));
        var session = await _app.History.GetWorkoutHistorySessionAsync(sessionId);
        Assert.NotNull(session);

        await _app.SessionExercises.DeleteAsync(session.Exercises[1].SessionExerciseId);

        var reloaded = await _app.History.GetWorkoutHistorySessionAsync(sessionId);
        Assert.NotNull(reloaded);
        Assert.Equal(_bench.Name, Assert.Single(reloaded.Exercises).ExerciseName);
        Assert.Equal(400, reloaded.TotalVolume);
    }
}
