using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using SQLite;

namespace MuscleMemory.Tests.Data;

public sealed class WorkoutSetRepositoryTests : IAsyncLifetime
{
    private const int MissingId = 999;
    private const int StaleSetNumber = 42;
    private const double HalfKilogramWeight = 62.5;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

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
    public async Task AddAsync_SeveralSets_NumbersThemFromOne()
    {
        var started = await _app.Data.StartAsync(_push);

        var sets = new List<WorkoutSet>();
        foreach (var weight in new[] { 80.0, 82.5, 85.0 })
        {
            sets.Add(await _app.Data.LogSetAsync(started.Exercises[0], weight, 5));
        }

        Assert.Equal([1, 2, 3], sets.Select(set => set.SetNumber));
        Assert.Equal([1, 2, 3], (await _app.Sets.GetForSessionExerciseAsync(started.Exercises[0].Id)).Select(set => set.SetNumber));
    }

    [Fact]
    public async Task AddAsync_IncomingSetNumber_IsReplacedByPosition()
    {
        var started = await _app.Data.StartAsync(_push);
        var set = new WorkoutSet { SessionExerciseId = started.Exercises[0].Id, Weight = 80, Reps = 5, SetNumber = StaleSetNumber };

        await _app.Sets.AddAsync(set);

        Assert.Equal(1, set.SetNumber);
    }

    [Fact]
    public async Task AddAsync_HalfKilogramWeight_RoundTripsExactly()
    {
        var started = await _app.Data.StartAsync(_push);

        await _app.Data.LogSetAsync(started.Exercises[0], HalfKilogramWeight, 8);

        Assert.Equal(HalfKilogramWeight, Assert.Single(await _app.Sets.GetForSessionExerciseAsync(started.Exercises[0].Id)).Weight);
    }

    [Fact]
    public async Task AddAsync_NoSessionExercise_ThrowsAndWritesNothing()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _app.Sets.AddAsync(new WorkoutSet { Weight = 80, Reps = 5 }));

        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task AddAsync_RenumberFails_LeavesNoSet()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Database.FailOnAsync(nameof(WorkoutSet), TableOperation.Update);

        await Assert.ThrowsAsync<SQLiteException>(() => _app.Data.LogSetAsync(started.Exercises[0], 80, 5));

        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task DeleteAsync_MiddleSet_RenumbersTheRestWithoutGaps()
    {
        var started = await _app.Data.StartAsync(_push);
        var first = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        var middle = await _app.Data.LogSetAsync(started.Exercises[0], 82.5, 5);
        var last = await _app.Data.LogSetAsync(started.Exercises[0], 85, 5);

        await _app.Sets.DeleteAsync(middle.Id);

        var remaining = await _app.Sets.GetForSessionExerciseAsync(started.Exercises[0].Id);
        Assert.Equal([(first.Id, 1), (last.Id, 2)], remaining.Select(set => (set.Id, set.SetNumber)));
    }

    [Fact]
    public async Task DeleteAsync_Set_LeavesOtherExercisesNumbering()
    {
        var started = await _app.Data.StartAsync(_push);
        var bench = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.LogSetAsync(started.Exercises[1], 40, 8);
        await _app.Data.LogSetAsync(started.Exercises[1], 42.5, 8);

        await _app.Sets.DeleteAsync(bench.Id);

        Assert.Equal([1, 2], (await _app.Sets.GetForSessionExerciseAsync(started.Exercises[1].Id)).Select(set => set.SetNumber));
    }

    [Fact]
    public async Task DeleteAsync_ThenAdd_ContinuesAfterTheLastSet()
    {
        var started = await _app.Data.StartAsync(_push);
        var first = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.LogSetAsync(started.Exercises[0], 82.5, 5);
        await _app.Sets.DeleteAsync(first.Id);

        var added = await _app.Data.LogSetAsync(started.Exercises[0], 85, 5);

        Assert.Equal(2, added.SetNumber);
    }

    [Fact]
    public async Task DeleteAsync_MissingSet_DoesNothing()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);

        await _app.Sets.DeleteAsync(MissingId);

        Assert.Equal(1, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task UpdateAsync_ExistingSet_ChangesWeightAndReps()
    {
        var started = await _app.Data.StartAsync(_push);
        var set = await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);

        await _app.Sets.UpdateAsync(set.Id, HalfKilogramWeight, 9);

        var stored = Assert.Single(await _app.Sets.GetForSessionExerciseAsync(started.Exercises[0].Id));
        Assert.Equal((HalfKilogramWeight, 9, 1), (stored.Weight, stored.Reps, stored.SetNumber));
    }

    [Fact]
    public async Task UpdateAsync_MissingSet_Throws() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => _app.Sets.UpdateAsync(MissingId, 80, 5));

    [Fact]
    public async Task GetForSessionExercisesAsync_SeveralExercises_ReturnsTheirSets()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);
        await _app.Data.LogSetAsync(started.Exercises[1], 40, 8);
        var other = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(other.Exercises[0], 90, 3);

        var sets = await _app.Sets.GetForSessionExercisesAsync([started.Exercises[0].Id, started.Exercises[1].Id]);

        Assert.Equal([40.0, 80.0], sets.Select(set => set.Weight).Order());
    }

    [Fact]
    public async Task GetForSessionExercisesAsync_NoIds_ReturnsNothing() =>
        Assert.Empty(await _app.Sets.GetForSessionExercisesAsync([]));

    [Fact]
    public async Task GetLastSessionSetsAsync_SeveralFinishedSessions_ReturnsTheNewestInOrder()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 70, 5));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 80, 5), new LoggedSet(0, 82.5, 4));
        _app.Clock.Advance(OneDay);
        var current = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(current.Exercises[0], 90, 3);

        var last = await _app.Sets.GetLastSessionSetsAsync(_bench.Id, current.SessionId);

        Assert.Equal([(1, 80.0), (2, 82.5)], last.Select(set => (set.SetNumber, set.Weight)));
    }

    [Fact]
    public async Task GetLastSessionSetsAsync_NewerSessionWithoutThatExercise_SkipsIt()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(1, 40, 8));
        var current = await _app.Data.StartAsync(_push);

        var last = await _app.Sets.GetLastSessionSetsAsync(_bench.Id, current.SessionId);

        Assert.Equal(80.0, Assert.Single(last).Weight);
    }

    [Fact]
    public async Task GetLastSessionSetsAsync_OpenOlderSession_IsIgnored()
    {
        var abandoned = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(abandoned.Exercises[0], 100, 1);
        var current = await _app.Data.StartAsync(_push);

        Assert.Empty(await _app.Sets.GetLastSessionSetsAsync(_bench.Id, current.SessionId));
    }

    [Fact]
    public async Task GetLastSessionSetsAsync_ExerciseTwiceInSession_ReturnsTheLaterPerformance()
    {
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _bench);
        await _app.Data.CompletedSessionAsync(twice, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 60, 12));
        var current = await _app.Data.StartAsync(_push);

        Assert.Equal(60.0, Assert.Single(await _app.Sets.GetLastSessionSetsAsync(_bench.Id, current.SessionId)).Weight);
    }

    [Fact]
    public async Task GetLastSessionSetsAsync_NoHistory_ReturnsNothing()
    {
        var current = await _app.Data.StartAsync(_push);

        Assert.Empty(await _app.Sets.GetLastSessionSetsAsync(_bench.Id, current.SessionId));
    }

    [Fact]
    public async Task GetRepMaxesAsync_SeveralSetsPerRepCount_ReturnsTheHeaviestOfEach()
    {
        var first = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 85, 5), new LoggedSet(0, 90, 3));
        var second = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 87.5, 5), new LoggedSet(0, 90, 3));

        var repMaxes = await _app.Sets.GetRepMaxesAsync([_bench.Id]);

        Assert.Equal(
            [(3, 90.0, first, TestApp.StartUtc), (5, 87.5, second, TestApp.StartUtc + OneDay)],
            repMaxes.OrderBy(set => set.Reps).Select(set => (set.Reps, set.Weight, set.SessionId, set.DateUtc)));
        Assert.All(repMaxes, set => Assert.Equal((DateTimeKind.Utc, _bench.Id), (set.DateUtc.Kind, set.ExerciseId)));
    }

    [Fact]
    public async Task GetRepMaxesAsync_SameWeightAndRepsInOneSession_ReturnsTheSetLoggedFirst()
    {
        var started = await _app.Data.StartAsync(_push);
        var first = await _app.Data.LogSetAsync(started.Exercises[0], 100, 5);
        await _app.Data.LogSetAsync(started.Exercises[0], 100, 5);
        await _app.ActiveSessions.FinishAsync(started.SessionId);

        var repMax = Assert.Single(await _app.Sets.GetRepMaxesAsync([_bench.Id]));

        Assert.Equal(first.Id, repMax.SetId);
    }

    [Fact]
    public async Task GetRepMaxesAsync_OpenSession_IsIgnored()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 120, 5);

        Assert.Equal(80, Assert.Single(await _app.Sets.GetRepMaxesAsync([_bench.Id])).Weight);
    }

    [Fact]
    public async Task GetRepMaxesAsync_DeletedSet_IsGone()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 120, 5));
        var heaviest = (await _app.Sets.GetRepMaxesAsync([_bench.Id])).Single();

        await _app.ActiveSessions.DeleteSetAsync(heaviest.SetId);

        Assert.Equal(80, Assert.Single(await _app.Sets.GetRepMaxesAsync([_bench.Id])).Weight);
    }

    [Fact]
    public async Task GetRepMaxesAsync_SetWithoutReps_IsIgnored()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 200, 0), new LoggedSet(0, 80, 5));

        var repMax = Assert.Single(await _app.Sets.GetRepMaxesAsync([_bench.Id]));

        Assert.Equal((80.0, 5), (repMax.Weight, repMax.Reps));
    }

    [Fact]
    public async Task GetRepMaxesAsync_SeveralExercises_KeepsThemApart()
    {
        var squat = await _app.Data.ExerciseAsync("Squat");
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _press, _bench);
        await _app.Data.CompletedSessionAsync(twice, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 50, 5), new LoggedSet(2, 85, 5));

        var repMaxes = await _app.Sets.GetRepMaxesAsync([_bench.Id, _press.Id, squat.Id]);

        Assert.Equal([(_bench.Id, 85.0), (_press.Id, 50.0)], repMaxes.OrderBy(set => set.ExerciseId).Select(set => (set.ExerciseId, set.Weight)));
    }

    [Fact]
    public async Task GetRepMaxesAsync_NoIds_ReturnsNothing() =>
        Assert.Empty(await _app.Sets.GetRepMaxesAsync([]));

    [Fact]
    public async Task GetSessionVolumesAsync_FinishedSessions_SumsEachSessionOfTheExercise()
    {
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _press, _bench);
        var first = await _app.Data.CompletedSessionAsync(twice, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 50, 10), new LoggedSet(2, 60, 10));
        var second = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 100, 3), new LoggedSet(0, 200, 0));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 100, 10);

        var volumes = await _app.Sets.GetSessionVolumesAsync(_bench.Id);

        Assert.Equal(
            [(first, TestApp.StartUtc, 1000.0), (second, TestApp.StartUtc + OneDay, 300.0)],
            volumes.OrderBy(volume => volume.SessionId).Select(volume => (volume.SessionId, volume.DateUtc, volume.Volume)));
        Assert.All(volumes, volume => Assert.Equal(DateTimeKind.Utc, volume.DateUtc.Kind));
    }

    [Fact]
    public async Task GetSessionVolumesAsync_NoHistory_ReturnsNothing() =>
        Assert.Empty(await _app.Sets.GetSessionVolumesAsync(_bench.Id));

    [Fact]
    public async Task GetSessionRepTotalsAsync_FinishedSessions_GroupsEachSessionByRepCount()
    {
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _press, _bench);
        var first = await _app.Data.CompletedSessionAsync(
            twice, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(1, 50, 5), new LoggedSet(2, 85, 5), new LoggedSet(2, 60, 10));
        var second = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 100, 3), new LoggedSet(0, 200, 0));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 120, 5);

        var totals = await _app.Sets.GetSessionRepTotalsAsync(_bench.Id);

        Assert.Equal(
            [(first, 5, 85.0, 825.0), (first, 10, 60.0, 600.0), (second, 3, 100.0, 300.0)],
            totals.OrderBy(total => total.SessionId).ThenBy(total => total.Reps).Select(total => (total.SessionId, total.Reps, total.HeaviestWeight, total.Volume)));
        Assert.Equal([TestApp.StartUtc, TestApp.StartUtc, TestApp.StartUtc + OneDay], totals.OrderBy(total => total.SessionId).Select(total => total.DateUtc));
        Assert.All(totals, total => Assert.Equal(DateTimeKind.Utc, total.DateUtc.Kind));
    }

    [Fact]
    public async Task GetSessionRepTotalsAsync_NoHistory_ReturnsNothing() =>
        Assert.Empty(await _app.Sets.GetSessionRepTotalsAsync(_bench.Id));

    [Fact]
    public async Task Clear_InTransaction_RemovesEverySet()
    {
        var started = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(started.Exercises[0], 80, 5);

        await (await _app.Database.ConnectionAsync()).RunInTransactionAsync(_app.Sets.Clear);

        Assert.Equal(0, await _app.Database.CountAsync(nameof(WorkoutSet)));
    }
}
