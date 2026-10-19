using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseHistoryViewModelTests : IAsyncLifetime
{
    private const string BenchPress = "Bench Press";
    private const string PushDay = "Push Day";
    private const string ChestDay = "Chest Day";

    private static readonly DateTime FirstLocalStart = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Local);
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private readonly TestViewModels _ui = new();
    private ExerciseHistoryViewModel _history = null!;
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;
    private Workout _chest = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest);
        _press = await _ui.App.Data.ExerciseAsync("Overhead Press", MuscleGroup.Shoulders);
        _push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);
        _chest = await _ui.App.Data.WorkoutAsync(ChestDay, _bench);
        _history = _ui.CreateExerciseHistory();
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public async Task Open_Exercise_ListsEverySessionWithItNewestFirst()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await OpenAsync(_bench);

        Assert.Equal(BenchPress, _history.ExerciseName);
        Assert.Equal("3 sessions · newest first", _history.SessionCountText);
        Assert.Equal(["Sat, Mar 7 · 09:15", "Fri, Mar 6 · 09:15", "Thu, Mar 5 · 09:15"], _history.History.Select(item => item.DateText));
        Assert.Equal([ChestDay, PushDay, PushDay], _history.History.Select(item => item.Entry.WorkoutName));
        Assert.Equal(["1 set", "1 set", "2 sets"], _history.History.Select(item => item.SetCountText));
        Assert.Equal("1,300 kg", _history.History[2].VolumeText);
        Assert.Equal(["Set 1, 80 kg, 8 reps", "Set 2, 82.5 kg, 8 reps"], _history.History[2].Sets.Select(set => set.Description));
    }

    [Fact]
    public async Task Open_PerformedWithoutSetsOrStillOpen_LeavesThoseOut()
    {
        await SeedAsync();
        var open = await _ui.App.Data.StartAsync(_push);
        await _ui.App.Data.LogSetAsync(open.Exercises[0], 100, 1);

        await OpenAsync(_press);

        Assert.Single(_history.History);
    }

    [Fact]
    public async Task Open_NeverPerformed_IsEmptyWithoutACount()
    {
        await OpenAsync(_press);

        Assert.Equal((true, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        Assert.Equal(string.Empty, _history.SessionCountText);
        Assert.Empty(_history.History);
        Assert.Null(_history.Records);
    }

    [Fact]
    public async Task Open_WeightedHistory_ShowsItsRecordsWithTheirDates()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await OpenAsync(_bench);

        var records = Assert.IsType<PersonalRecordsItem>(_history.Records).Records;
        Assert.Equal(
            [
                ("90 kg × 3", "Heaviest weight\nMar 07, 2026"),
                ("104.5 kg", "Estimated 1RM\nMar 05, 2026"),
                ("1,300 kg", "Best session volume\nMar 05, 2026")
            ],
            records.Select(record => (record.ValueText, record.Caption)));
        Assert.Equal("Heaviest weight: 90 kg for 3 reps, achieved on Mar 07, 2026.", records[0].Description);
    }

    [Fact]
    public async Task Open_BodyweightHistory_ShowsOnlyMostReps()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var dips = await _ui.App.Data.ExerciseAsync("Dips", MuscleGroup.Triceps, EquipmentType.None);
        var bodyweight = await _ui.App.Data.WorkoutAsync("Bodyweight", dips);
        await _ui.App.Data.CompletedSessionAsync(bodyweight, FirstLocalStart.ToUniversalTime(), new LoggedSet(0, 0, 12), new LoggedSet(0, 0, 15));

        await OpenAsync(dips);

        var record = Assert.Single(Assert.IsType<PersonalRecordsItem>(_history.Records).Records);
        Assert.Equal(("15", "Most reps in a set\nMar 05, 2026"), (record.ValueText, record.Caption));
    }

    [Fact]
    public async Task Open_OnlyAnOpenSession_HasNoRecords()
    {
        var open = await _ui.App.Data.StartAsync(_push);
        await _ui.App.Data.LogSetAsync(open.Exercises[0], 100, 5);

        await OpenAsync(_bench);

        Assert.Null(_history.Records);
        Assert.True(_history.ListState.IsEmpty);
    }

    [Fact]
    public async Task Open_BeforeTheLoadEnds_ClaimsNeitherItemsNorEmpty()
    {
        await SeedAsync();

        _history.ApplyQueryAttributes(Query(_bench));

        Assert.Equal((false, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        await _ui.SettleAsync();
        Assert.True(_history.ListState.HasItems);
    }

    [Fact]
    public async Task Open_InvalidId_LoadsNothing()
    {
        await SeedAsync();

        _history.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.ExerciseId] = 0, [QueryKeys.ExerciseName] = BenchPress });
        await _ui.SettleAsync();

        Assert.Equal(BenchPress, _history.ExerciseName);
        Assert.Empty(_history.History);
        Assert.Equal((false, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
    }

    [Theory]
    [InlineData(1, "1 sesja · od najnowszych")]
    [InlineData(3, "3 sesje · od najnowszych")]
    [InlineData(5, "5 sesji · od najnowszych")]
    public async Task Open_Polish_CountsSessionsWithPolishPlurals(int sessions, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);
        for (var day = 0; day < sessions; day++)
        {
            await _ui.App.Data.CompletedSessionAsync(_chest, FirstLocalStart.ToUniversalTime() + (day * OneDay), new LoggedSet(0, 82.5, 8));
        }

        await OpenAsync(_bench);

        Assert.Equal(expected, _history.SessionCountText);
        Assert.Equal("czw., 5 mar · 09:15", _history.History[^1].DateText);
        Assert.Equal("660 kg", _history.History[^1].VolumeText);
    }

    [Fact]
    public async Task DataChangedElsewhere_Notified_KeepsTheLoadedHistory()
    {
        await SeedAsync();
        await OpenAsync(_bench);
        await _ui.App.Data.CompletedSessionAsync(_chest, _ui.Clock.GetUtcNow().UtcDateTime.AddDays(1), new LoggedSet(0, 95, 2));

        _ui.App.DataChanges.Notify(DataArea.All);
        await _ui.SettleAsync();

        Assert.Equal(3, _history.History.Count);
    }

    [Fact]
    public async Task GoBack_Always_NavigatesBack()
    {
        await _history.GoBackCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
    }

    private async Task SeedAsync()
    {
        var start = FirstLocalStart.ToUniversalTime();
        await _ui.App.Data.CompletedSessionAsync(_push, start, new LoggedSet(0, 80, 8), new LoggedSet(0, 82.5, 8), new LoggedSet(1, 50, 10));
        await _ui.App.Data.CompletedSessionAsync(_push, start + OneDay, new LoggedSet(0, 85, 5));
        await _ui.App.Data.CompletedSessionAsync(_chest, start + (2 * OneDay), new LoggedSet(0, 90, 3));
    }

    private async Task OpenAsync(Exercise exercise)
    {
        _history.ApplyQueryAttributes(Query(exercise));
        await _ui.SettleAsync();
    }

    private static Dictionary<string, object> Query(Exercise exercise) => new()
    {
        [QueryKeys.ExerciseId] = exercise.Id,
        [QueryKeys.ExerciseName] = exercise.Name
    };
}
