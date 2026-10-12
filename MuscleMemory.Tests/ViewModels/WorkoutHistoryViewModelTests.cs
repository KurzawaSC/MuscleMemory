using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class WorkoutHistoryViewModelTests : IAsyncLifetime
{
    private const string PushDay = "Push Day";
    private const string BenchPress = "Bench Press";
    private const string OverheadPress = "Overhead Press";

    private static readonly DateTime MorningOfTheFifth = new(2026, 3, 5, 9, 15, 0, DateTimeKind.Local);
    private static readonly DateTime EveningOfTheFifth = new(2026, 3, 5, 18, 0, 0, DateTimeKind.Local);
    private static readonly DateTime MorningOfTheSeventh = new(2026, 3, 7, 10, 0, 0, DateTimeKind.Local);

    private readonly TestViewModels _ui = new();
    private WorkoutHistoryViewModel _history = null!;
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;
    private int _morning;
    private int _evening;
    private int _latest;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest);
        _press = await _ui.App.Data.ExerciseAsync(OverheadPress, MuscleGroup.Shoulders);
        _push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);
        _history = _ui.CreateWorkoutHistory();
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public async Task Open_Workout_ShowsItsSessionsNewestFirstAndSelectsTheNewest()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await OpenAsync();

        Assert.Equal("Push Day · history", _history.HeaderTitle);
        Assert.Equal((false, true), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        Assert.Equal([_latest, _evening, _morning], _history.Sessions.Select(item => item.Session.SessionId));
        Assert.Equal(_latest, _history.SelectedSession?.Session.SessionId);
    }

    [Fact]
    public async Task Open_SessionsSharingADay_AddTheTimeToTheirChips()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await OpenAsync();

        Assert.Equal(["Mar 07", "Mar 05 · 18:00", "Mar 05 · 09:15"], _history.Sessions.Select(item => item.ChipText));
        Assert.Equal(["Mar 07, 2026", "Mar 05, 2026", "Mar 05, 2026"], _history.Sessions.Select(item => item.DateText));
    }

    [Fact]
    public async Task Open_Sessions_ShowTheirDurationSetCountAndVolume()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await OpenAsync();

        Assert.Equal(["Time 30:00 · 1 set", "Time 1:02:03 · 1 set", "Time 47:05 · 3 sets"], _history.Sessions.Select(item => item.StatsText));
        Assert.Equal(["270", "425", "1,800"], _history.Sessions.Select(item => item.VolumeText));
        Assert.Equal([BenchPress, OverheadPress], _history.Sessions[2].Exercises.Select(exercise => exercise.Exercise.ExerciseName));
        Assert.Equal(["Set 1, 80 kg, 8 reps", "Set 2, 82.5 kg, 8 reps"], _history.Sessions[2].Exercises[0].Sets.Select(set => set.Description));
    }

    [Fact]
    public async Task Open_Polish_UsesPolishPatternsAndPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);
        await SeedAsync();

        await OpenAsync();

        Assert.Equal("Push Day · historia", _history.HeaderTitle);
        Assert.Equal(["7 mar", "5 mar · 18:00", "5 mar · 09:15"], _history.Sessions.Select(item => item.ChipText));
        Assert.Equal("Czas 47:05 · 3 serie", _history.Sessions[2].StatsText);
        Assert.Equal($"1{Cultures.PolishGroupSeparator}800", _history.Sessions[2].VolumeText);
    }

    [Fact]
    public async Task Open_NoFinishedSessions_IsEmpty()
    {
        await _ui.App.Data.StartAsync(_push);

        await OpenAsync();

        Assert.Equal((true, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        Assert.Empty(_history.Sessions);
        Assert.Null(_history.SelectedSession);
    }

    [Fact]
    public async Task Open_BeforeTheLoadEnds_ClaimsNeitherItemsNorEmpty()
    {
        await SeedAsync();

        _history.ApplyQueryAttributes(Query(_push.Id));

        Assert.Equal((false, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        await _ui.SettleAsync();
        Assert.True(_history.ListState.HasItems);
    }

    [Fact]
    public async Task DeleteSet_Confirmed_RemovesItAndKeepsTheSelection()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await OpenAsync();
        _history.SelectedSession = _history.Sessions[2];
        _ui.AnswerConfirmations(true);

        _history.SetActions.ShowCommand.Execute(_history.Sessions[2].Exercises[0].Sets[0].Set);
        await _history.SetActions.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(_morning, _history.SelectedSession?.Session.SessionId);
        Assert.Equal("Time 47:05 · 2 sets", _history.SelectedSession?.StatsText);
        Assert.Equal(["Set 1, 82.5 kg, 8 reps"], _history.SelectedSession?.Exercises[0].Sets.Select(set => set.Description));
        Assert.Equal([82.5], (await BenchSetsAsync(_morning)).Select(set => set.Weight));
    }

    [Fact]
    public async Task DeleteSet_Cancelled_KeepsIt()
    {
        await SeedAsync();
        await OpenAsync();
        _ui.AnswerConfirmations(false);

        _history.SetActions.ShowCommand.Execute(_history.Sessions[0].Exercises[0].Sets[0].Set);
        await _history.SetActions.DeleteCommand.ExecuteAsync(null);

        Assert.Single(_history.Sessions[0].Exercises[0].Sets);
        Assert.Single(await BenchSetsAsync(_latest));
    }

    [Fact]
    public async Task EditSet_Saved_UpdatesTheStoredSetAndTheSession()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await OpenAsync();

        _history.SetActions.ShowCommand.Execute(_history.Sessions[0].Exercises[0].Sets[0].Set);
        await _history.SetActions.EditCommand.ExecuteAsync(null);
        _history.SetActions.Editor.RepsInput = "5";
        await _history.SetActions.SaveEditorCommand.ExecuteAsync(null);

        Assert.False(_history.SetActions.IsEditorOpen);
        Assert.Equal("450", _history.Sessions[0].VolumeText);
        Assert.Equal(5, Assert.Single(await BenchSetsAsync(_latest)).Reps);
    }

    [Fact]
    public async Task AddSet_Saved_AppendsItPrefilledFromTheLastSet()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await OpenAsync();
        var bench = _history.Sessions[2].Exercises[0];

        _history.AddSetCommand.Execute(bench);
        Assert.Equal(("Add set", "Add", "82.5", "8"), (_history.SetActions.EditorTitle, _history.SetActions.EditorConfirmText, _history.SetActions.Editor.WeightInput, _history.SetActions.Editor.RepsInput));
        await _history.SetActions.SaveEditorCommand.ExecuteAsync(null);

        Assert.Equal("Time 47:05 · 4 sets", _history.Sessions[2].StatsText);
        Assert.Equal([1, 2, 3], (await BenchSetsAsync(_morning)).Select(set => set.SetNumber));
    }

    [Fact]
    public async Task RemoveExercise_Confirmed_RemovesItFromTheSession()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await OpenAsync();
        _history.SelectedSession = _history.Sessions[2];
        _ui.AnswerConfirmations(true);

        await RemoveExerciseAsync(_history.Sessions[2].Exercises[1]);

        await _ui.Dialogs.Received(1).ConfirmAsync(
            AppResources.TitleRemoveExercise, "Are you sure you want to remove “Overhead Press”?", AppResources.ButtonRemove, AppResources.ButtonCancel);
        Assert.Equal([BenchPress], _history.SelectedSession?.Exercises.Select(exercise => exercise.Exercise.ExerciseName));
        Assert.False(_history.IsExerciseActionSheetOpen);
        Assert.Single(await _ui.App.SessionExercises.GetForSessionAsync(_morning));
    }

    [Fact]
    public async Task RemoveExercise_Cancelled_KeepsIt()
    {
        await SeedAsync();
        await OpenAsync();
        _ui.AnswerConfirmations(false);

        await RemoveExerciseAsync(_history.Sessions[0].Exercises[0]);

        Assert.Equal(2, _history.Sessions[0].Exercises.Count);
        Assert.Equal(2, (await _ui.App.SessionExercises.GetForSessionAsync(_latest)).Count);
    }

    [Fact]
    public async Task RemoveExercise_LastOneOfASession_DropsTheSessionEverywhere()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await OpenAsync();
        _history.SelectedSession = _history.Sessions[1];
        _ui.AnswerConfirmations(true);

        await RemoveExerciseAsync(_history.Sessions[1].Exercises[0]);
        await RemoveExerciseAsync(_history.Sessions[1].Exercises[0]);

        Assert.Equal([_latest, _morning], _history.Sessions.Select(item => item.Session.SessionId));
        Assert.Equal(["Mar 07", "Mar 05"], _history.Sessions.Select(item => item.ChipText));
        Assert.Equal(_latest, _history.SelectedSession?.Session.SessionId);
        Assert.Null(await _ui.App.Sessions.GetAsync(_evening));
        Assert.Equal(2, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
    }

    [Fact]
    public async Task RemoveExercise_LastOneOfTheOnlySession_ShowsTheEmptyState()
    {
        var curls = await _ui.App.Data.WorkoutAsync("Curls", _bench);
        await _ui.App.Data.CompletedSessionAsync(curls, MorningOfTheFifth.ToUniversalTime(), new LoggedSet(0, 30, 12));
        await OpenAsync(curls);
        _ui.AnswerConfirmations(true);

        await RemoveExerciseAsync(_history.Sessions[0].Exercises[0]);

        Assert.Equal((true, false), (_history.ListState.IsEmpty, _history.ListState.HasItems));
        Assert.Null(_history.SelectedSession);
        Assert.Equal(0, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
    }

    [Fact]
    public async Task PickExercise_IntoTheSelectedSession_AppendsIt()
    {
        await SeedAsync();
        var squat = await _ui.App.Data.ExerciseAsync("Back Squat", MuscleGroup.Legs);
        await OpenAsync();

        await _history.AddExerciseCommand.ExecuteAsync(null);
        Assert.True(_history.IsExercisePickerOpen);
        await _history.PickExerciseCommand.ExecuteAsync(squat);

        Assert.False(_history.IsExercisePickerOpen);
        Assert.Equal([BenchPress, OverheadPress, "Back Squat"], _history.SelectedSession?.Exercises.Select(exercise => exercise.Exercise.ExerciseName));
        Assert.Equal(3, (await _ui.App.SessionExercises.GetForSessionAsync(_latest)).Count);
    }

    [Fact]
    public async Task LongPressExercise_Item_ClicksAndOpensItsActions()
    {
        await SeedAsync();
        await OpenAsync();
        var bench = _history.Sessions[0].Exercises[0];

        _history.LongPressExerciseCommand.Execute(bench);

        _ui.Haptics.Received(1).Click();
        Assert.True(_history.IsExerciseActionSheetOpen);
        Assert.Same(bench.Exercise, _history.ActionExercise);
    }

    [Fact]
    public async Task DataChangedElsewhere_Notified_KeepsTheLoadedHistory()
    {
        await SeedAsync();
        await OpenAsync();
        await _ui.App.Data.CompletedSessionAsync(_push, _ui.Clock.GetUtcNow().UtcDateTime.AddDays(1), new LoggedSet(0, 95, 2));

        _ui.App.DataChanges.Notify(DataArea.All);
        await _ui.SettleAsync();

        Assert.Equal(3, _history.Sessions.Count);
    }

    [Fact]
    public async Task NavigateBack_SheetOpen_OnlyClosesIt()
    {
        await SeedAsync();
        await OpenAsync();
        _history.ShowExerciseActionsCommand.Execute(_history.Sessions[0].Exercises[0]);

        await _history.NavigateBackCommand.ExecuteAsync(null);

        Assert.False(_history.IsExerciseActionSheetOpen);
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    [Fact]
    public async Task NavigateBack_NothingOpen_GoesBack()
    {
        await OpenAsync();

        await _history.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
    }

    private async Task SeedAsync()
    {
        _morning = await _ui.App.Data.CompletedSessionAsync(
            _push, MorningOfTheFifth.ToUniversalTime(), new TimeSpan(0, 47, 5), new LoggedSet(0, 80, 8), new LoggedSet(0, 82.5, 8), new LoggedSet(1, 50, 10));
        _evening = await _ui.App.Data.CompletedSessionAsync(
            _push, EveningOfTheFifth.ToUniversalTime(), new TimeSpan(1, 2, 3), new LoggedSet(0, 85, 5));
        _latest = await _ui.App.Data.CompletedSessionAsync(
            _push, MorningOfTheSeventh.ToUniversalTime(), TimeSpan.FromMinutes(30), new LoggedSet(0, 90, 3));
    }

    private Task OpenAsync() => OpenAsync(_push);

    private async Task OpenAsync(Workout workout)
    {
        _history.ApplyQueryAttributes(Query(workout.Id, workout.Name));
        await _ui.SettleAsync();
    }

    private static Dictionary<string, object> Query(int workoutId, string workoutName = PushDay) => new()
    {
        [QueryKeys.WorkoutId] = workoutId,
        [QueryKeys.WorkoutName] = workoutName
    };

    private async Task RemoveExerciseAsync(WorkoutHistoryExerciseItem exercise)
    {
        _history.ShowExerciseActionsCommand.Execute(exercise);
        await _history.DeleteActionExerciseCommand.ExecuteAsync(null);
    }

    private async Task<List<WorkoutSet>> BenchSetsAsync(int sessionId)
    {
        var bench = (await _ui.App.SessionExercises.GetForSessionAsync(sessionId))[0];
        return await _ui.App.Sets.GetForSessionExerciseAsync(bench.Id);
    }
}
