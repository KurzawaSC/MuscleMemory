using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

public sealed class ExerciseProgressServiceTests : IAsyncLifetime
{
    private const int Seed = 20261019;
    private const int GeneratedSessions = 60;
    private const int MostSetsPerSession = 6;
    private const double LightestWeight = 60;
    private const double WeightStep = 2.5;
    private const int WeightSteps = 6;
    private const int MostGeneratedReps = 15;
    private const int SessionsPerDay = 2;
    private const double Bodyweight = 0;

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
    private static readonly TimeSpan SameDayGap = TimeSpan.FromHours(3);

    private readonly TestApp _app = new();
    private Exercise _bench = null!;
    private Exercise _dips = null!;
    private Workout _push = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _app.Data.ExerciseAsync("Bench Press");
        _dips = await _app.Data.ExerciseAsync("Dips", equipment: EquipmentType.None);
        _push = await _app.Data.WorkoutAsync("Push Day", _bench, _dips);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task GetProgressAsync_ManySetsWithTies_EqualsTheSeriesOfEveryLoggedSet()
    {
        var sets = await LogGeneratedHistoryAsync(exerciseIndex: 0, LoggedWeight);

        var progress = await _app.Progress.GetProgressAsync(_bench.Id);

        Assert.Equal(BruteForce(sets, BestEstimate), progress.OneRepMax);
        Assert.Equal(BruteForce(sets, VolumeOf), progress.Volume);
        Assert.Empty(progress.Reps);
    }

    [Fact]
    public async Task GetProgressAsync_BodyweightHistory_EqualsTheMostRepsOfEveryLoggedSet()
    {
        var sets = await LogGeneratedHistoryAsync(exerciseIndex: 1, _ => Bodyweight);

        var progress = await _app.Progress.GetProgressAsync(_dips.Id);

        Assert.Equal(BruteForce(sets, MostReps), progress.Reps);
        Assert.Empty(progress.OneRepMax);
        Assert.Empty(progress.Volume);
    }

    [Fact]
    public async Task GetProgressAsync_GeneratedHistory_AgreesExactlyWithThePersonalRecords()
    {
        await LogGeneratedHistoryAsync(exerciseIndex: 0, LoggedWeight);

        var progress = await _app.Progress.GetProgressAsync(_bench.Id);
        var records = await _app.Records.GetRecordsAsync(_bench.Id);
        var volumes = await _app.Sets.GetSessionVolumesAsync(_bench.Id);

        Assert.Equal(records.BestOneRepMax?.Value, progress.OneRepMax.Max(point => point.Value));
        Assert.Equal(records.BestSessionVolume?.Volume, progress.Volume.Max(point => point.Value));
        Assert.Equal(
            volumes.Where(volume => volume.Volume > 0).OrderBy(volume => volume.SessionId).Select(volume => (volume.SessionId, volume.Volume)),
            progress.Volume.OrderBy(point => point.SessionId).Select(point => (point.SessionId, point.Value)));
    }

    [Fact]
    public async Task GetProgressAsync_LocalZoneEastOfUtc_DatesSessionsByTheirLocalStart()
    {
        _app.Clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("UTC+10", TimeSpan.FromHours(10), "UTC+10", "UTC+10"));
        var lateUtc = new DateTime(2026, 3, 2, 20, 0, 0, DateTimeKind.Utc);
        await _app.Data.CompletedSessionAsync(_push, lateUtc, new LoggedSet(0, 100, 5));

        var progress = await _app.Progress.GetProgressAsync(_bench.Id);

        Assert.Equal(new DateOnly(2026, 3, 3), Assert.Single(progress.Volume).Date);
    }

    [Fact]
    public async Task GetProgressAsync_NoHistory_HasNoMetrics() =>
        Assert.False((await _app.Progress.GetProgressAsync(_bench.Id)).HasAny);

    private static double LoggedWeight(Random random) => LightestWeight + (WeightStep * random.Next(WeightSteps));

    private async Task<List<(int SessionId, DateTime StartUtc, double Weight, int Reps)>> LogGeneratedHistoryAsync(int exerciseIndex, Func<Random, double> weightOf)
    {
        var random = new Random(Seed);
        List<(int SessionId, DateTime StartUtc, double Weight, int Reps)> sets = [];

        for (var index = 0; index < GeneratedSessions; index++)
        {
            var startUtc = TestApp.StartUtc + ((index / SessionsPerDay) * OneDay) + ((index % SessionsPerDay) * SameDayGap);
            _app.Clock.SetUtcNow(startUtc);
            var session = await _app.Data.StartAsync(_push);
            var setCount = random.Next(1, MostSetsPerSession + 1);
            for (var set = 0; set < setCount; set++)
            {
                var logged = await _app.Data.LogSetAsync(session.Exercises[exerciseIndex], weightOf(random), random.Next(0, MostGeneratedReps + 1));
                sets.Add((session.SessionId, startUtc, logged.Weight, logged.Reps));
            }

            await _app.ActiveSessions.FinishAsync(session.SessionId);
        }

        return sets;
    }

    private static List<ProgressPoint> BruteForce(
        List<(int SessionId, DateTime StartUtc, double Weight, int Reps)> sets,
        Func<IEnumerable<(int SessionId, DateTime StartUtc, double Weight, int Reps)>, double?> valueOf) =>
        [
            .. sets.Where(set => set.Reps >= PersonalRecords.MinReps)
                   .GroupBy(set => set.SessionId)
                   .OrderBy(session => session.First().StartUtc)
                   .ThenBy(session => session.Key)
                   .SelectMany(session => valueOf(session) is { } value
                       ? [new ProgressPoint(session.Key, DateOnly.FromDateTime(session.First().StartUtc), value)]
                       : Array.Empty<ProgressPoint>())
        ];

    private static double? BestEstimate(IEnumerable<(int SessionId, DateTime StartUtc, double Weight, int Reps)> session) =>
        session.Max(set => OneRepMax.Estimate(set.Weight, set.Reps));

    private static double? VolumeOf(IEnumerable<(int SessionId, DateTime StartUtc, double Weight, int Reps)> session) =>
        session.Sum(set => set.Weight * set.Reps) is var volume && volume > 0 ? volume : null;

    private static double? MostReps(IEnumerable<(int SessionId, DateTime StartUtc, double Weight, int Reps)> session) =>
        session.Max(set => set.Reps);
}
