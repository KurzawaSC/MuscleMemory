using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class PersonalRecordServiceTests : IAsyncLifetime
{
    private const int Seed = 20261019;
    private const int GeneratedSessions = 40;
    private const int SetsPerSession = 5;
    private const double LightestWeight = 60;
    private const double WeightStep = 2.5;
    private const int WeightSteps = 8;
    private const int MostGeneratedReps = 15;
    private const double Bodyweight = 0;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Exercise _dips = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _press = await _app.Data.ExerciseAsync("Overhead Press");
        _dips = await _app.Data.ExerciseAsync("Dips", equipment: EquipmentType.None);
        _push = await _app.Data.WorkoutAsync("Push Day", _bench, _press, _dips);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetRecordsAsync_FinishedHistory_FindsEveryRecord()
    {
        var first = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 8), new LoggedSet(0, 85, 5));
        var second = await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(0, 90, 3), new LoggedSet(0, 87.5, 6));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 120, 5);

        var records = await _app.Records.GetRecordsAsync(_bench.Id);

        Assert.Equal((90.0, 3, second), (records.HeaviestWeight?.Weight, records.HeaviestWeight?.Reps, records.HeaviestWeight?.SessionId));
        Assert.Equal((105.0, 87.5, TestApp.StartUtc + OneDay), (records.BestOneRepMax?.Value, records.BestOneRepMax?.Set.Weight, records.BestOneRepMax?.Set.DateUtc));
        Assert.Equal((first, 1065.0, TestApp.StartUtc), (records.BestSessionVolume?.SessionId, records.BestSessionVolume?.Volume, records.BestSessionVolume?.DateUtc));
        Assert.Null(records.MostReps);
    }

    [Fact]
    public async Task GetRecordsAsync_ManySetsWithTies_MatchTheRecordsOfEveryLoggedSet()
    {
        var (sets, volumes) = await LogGeneratedHistoryAsync();

        var records = await _app.Records.GetRecordsAsync(_bench.Id);

        Assert.Equal(PersonalRecords.Find(sets, volumes), records);
    }

    [Fact]
    public async Task GetRecordsAsync_BodyweightExercise_FindsOnlyMostReps()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(2, Bodyweight, 12), new LoggedSet(2, Bodyweight, 10));
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc + OneDay, new LoggedSet(2, Bodyweight, 12));

        var records = await _app.Records.GetRecordsAsync(_dips.Id);

        Assert.Equal((12, TestApp.StartUtc), (records.MostReps?.Reps, records.MostReps?.DateUtc));
        Assert.Equal((null, null, null), (records.HeaviestWeight, records.BestOneRepMax, records.BestSessionVolume));
    }

    [Fact]
    public async Task GetRecordsAsync_OnlyAnOpenSession_HasNoRecords()
    {
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 100, 5);

        Assert.False((await _app.Records.GetRecordsAsync(_bench.Id)).HasAny);
    }

    [Fact]
    public async Task FindNewRecordsAsync_OpenSession_MarksSetsBeatingTheFinishedHistory()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 100, 5));
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 100, 5);
        var record = await _app.Data.LogSetAsync(open.Exercises[0], 102.5, 5);
        await _app.Data.LogSetAsync(open.Exercises[0], 102.5, 5);

        var marked = await _app.Records.FindNewRecordsAsync(open.Exercises);

        Assert.Equal([record.Id], marked);
    }

    [Fact]
    public async Task FindNewRecordsAsync_ExerciseTwiceInTheSession_CarriesTheRunningBestAcross()
    {
        var twice = await _app.Data.WorkoutAsync("Bench Twice", _bench, _press, _bench);
        await _app.Data.CompletedSessionAsync(twice, TestApp.StartUtc, new LoggedSet(0, 100, 5));
        var open = await _app.Data.StartAsync(twice);
        var first = await _app.Data.LogSetAsync(open.Exercises[0], 102.5, 5);
        await _app.Data.LogSetAsync(open.Exercises[2], 102.5, 5);
        var heavier = await _app.Data.LogSetAsync(open.Exercises[2], 105, 5);

        var marked = await _app.Records.FindNewRecordsAsync(open.Exercises);

        Assert.Equal([first.Id, heavier.Id], marked.Order());
    }

    [Fact]
    public async Task FindNewRecordsAsync_NoFinishedHistory_MarksNothing()
    {
        var open = await _app.Data.StartAsync(_push);
        await _app.Data.LogSetAsync(open.Exercises[0], 60, 5);
        await _app.Data.LogSetAsync(open.Exercises[0], 100, 5);

        Assert.Empty(await _app.Records.FindNewRecordsAsync(open.Exercises));
    }

    [Fact]
    public async Task FindNewRecordsAsync_RecordDeleted_TheNextBestSetBecomesTheRecord()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 100, 5));
        var open = await _app.Data.StartAsync(_push);
        var record = await _app.Data.LogSetAsync(open.Exercises[0], 105, 5);
        var next = await _app.Data.LogSetAsync(open.Exercises[0], 102.5, 5);
        Assert.Equal([record.Id], await _app.Records.FindNewRecordsAsync(open.Exercises));

        await _app.ActiveSessions.DeleteSetAsync(record.Id);

        Assert.Equal([next.Id], await _app.Records.FindNewRecordsAsync(open.Exercises));
    }

    [Fact]
    public async Task FindNewRecordsAsync_RecordEditedDown_IsNoLongerARecord()
    {
        await _app.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 100, 5));
        var open = await _app.Data.StartAsync(_push);
        var record = await _app.Data.LogSetAsync(open.Exercises[0], 105, 5);

        await _app.ActiveSessions.UpdateSetAsync(record.Id, 100, 5);

        Assert.Empty(await _app.Records.FindNewRecordsAsync(open.Exercises));
    }

    [Fact]
    public async Task FindNewRecordsAsync_NoExercises_MarksNothing() =>
        Assert.Empty(await _app.Records.FindNewRecordsAsync([]));

    private async Task<(List<PerformedSet> Sets, List<SessionVolume> Volumes)> LogGeneratedHistoryAsync()
    {
        var random = new Random(Seed);
        List<PerformedSet> sets = [];
        List<SessionVolume> volumes = [];

        for (var day = 0; day < GeneratedSessions; day++)
        {
            var startUtc = TestApp.StartUtc + (day * OneDay);
            _app.Clock.SetUtcNow(startUtc);
            var session = await _app.Data.StartAsync(_push);
            for (var index = 0; index < SetsPerSession; index++)
            {
                var weight = LightestWeight + (WeightStep * random.Next(WeightSteps));
                var logged = await _app.Data.LogSetAsync(session.Exercises[0], weight, random.Next(1, MostGeneratedReps + 1));
                sets.Add(new PerformedSet
                {
                    SetId = logged.Id,
                    SessionId = session.SessionId,
                    ExerciseId = _bench.Id,
                    DateUtc = startUtc,
                    Weight = logged.Weight,
                    Reps = logged.Reps
                });
            }

            await _app.ActiveSessions.FinishAsync(session.SessionId);
            volumes.Add(new SessionVolume
            {
                SessionId = session.SessionId,
                DateUtc = startUtc,
                Volume = sets.Where(set => set.SessionId == session.SessionId).Sum(set => set.Weight * set.Reps)
            });
        }

        return (sets, volumes);
    }
}
