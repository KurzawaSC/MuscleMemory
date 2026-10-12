using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

[Collection(nameof(DefaultCultureCollection))]
public sealed class ActiveWorkoutViewModelTests : IAsyncLifetime
{
    private const int TimeoutMilliseconds = 10_000;
    private const string PushDay = "Push Day";
    private const string LegDay = "Leg Day";
    private const string BenchPress = "Bench Press";
    private const string OverheadPress = "Overhead Press";
    private const int RestSeconds = TestDataBuilder.BreakSeconds;

    private readonly TestViewModels _ui = new();
    private Exercise _bench = null!;
    private Exercise _press = null!;
    private Workout _push = null!;

    private ActiveWorkoutViewModel Workout => _ui.ActiveWorkout;

    private DateTime Now => _ui.Clock.GetUtcNow().UtcDateTime;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest, EquipmentType.Barbell);
        _press = await _ui.App.Data.ExerciseAsync(OverheadPress, MuscleGroup.Shoulders, EquipmentType.Barbell);
        _push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public async Task Start_Workout_ShowsTheFirstExerciseAndItsPlan()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await StartAsync(_push);

        Assert.True(Workout.IsWorkoutActive);
        Assert.Equal(PushDay, Workout.WorkoutTitle);
        Assert.Equal([BenchPress, OverheadPress], Workout.Exercises.Select(exercise => exercise.ExerciseName));
        Assert.Equal(BenchPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal("Exercise 1 of 2 · Set 1 of 3", Workout.ProgressCaption);
        Assert.Equal("Target 8 reps · RPE 7", Workout.TargetText);
        Assert.Equal((false, true), (Workout.HasPreviousExercise, Workout.HasNextExercise));
        Assert.Equal([new LevelSegment(1, false), new LevelSegment(2, false), new LevelSegment(3, false)], Workout.SetSegments);
        Assert.Equal("00:00", Workout.TimerText);
        _ui.WorkoutTicker.Received(1).Start();
    }

    [Fact]
    public async Task Start_Workout_SavesTheStateForCrashRecovery()
    {
        await StartAsync(_push);

        var state = await _ui.App.ActiveState.GetAsync();
        Assert.NotNull(state);
        Assert.Equal((0, false, Now), (state.CurrentExerciseIndex, state.IsResting, state.StartTimeUtc));
        Assert.Null((await _ui.App.Sessions.GetAsync(state.SessionId))?.EndTimeUtc);
        Assert.Equal(Workout.CurrentExercise.WorkoutSessionId, state.SessionId);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task Start_DuringTheNavigationPause_ActivatesAndSavesOnlyAfterIt()
    {
        var cancellation = TestContext.Current.CancellationToken;
        _ui.Clock.HoldPauses();

        Open(_push);
        await _ui.Clock.PauseStarted.WaitAsync(cancellation);

        Assert.Equal(2, Workout.Exercises.Count);
        Assert.False(Workout.IsWorkoutActive);
        Assert.False(Workout.IsBannerVisible);
        Assert.Null(await _ui.App.ActiveState.GetAsync());
        _ui.Clock.ReleasePauses();
        await _ui.SettleAsync().WaitAsync(cancellation);
        Assert.True(Workout.IsWorkoutActive);
        Assert.NotNull(await _ui.App.ActiveState.GetAsync());
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task Start_SecondRequestWhileStarting_IsIgnored()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var legs = await _ui.App.Data.WorkoutAsync(LegDay, _press);
        _ui.Clock.HoldPauses();

        Open(_push);
        await _ui.Clock.PauseStarted.WaitAsync(cancellation);
        Open(legs);
        _ui.Clock.ReleasePauses();
        await _ui.SettleAsync().WaitAsync(cancellation);

        Assert.Equal(PushDay, Workout.WorkoutTitle);
        Assert.Equal(1, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
    }

    [Fact]
    public async Task Start_AnotherWorkoutWhileOneIsActive_KeepsTheActiveSession()
    {
        var legs = await _ui.App.Data.WorkoutAsync(LegDay, _press);
        await StartAsync(_push);

        await StartAsync(legs);

        Assert.Equal(PushDay, Workout.WorkoutTitle);
        Assert.Equal(1, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
    }

    [Fact]
    public async Task Start_ExerciseNeverDone_ShowsFirstTimeAndLeavesTheInputsEmpty()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await StartAsync(_push);

        Assert.Equal("First time", Workout.LastSessionResultsText);
        Assert.False(Workout.HasLastSession);
        Assert.Equal((string.Empty, string.Empty), (Workout.SetInput.WeightInput, Workout.SetInput.RepsInput));
        Assert.False(Workout.SaveSetCommand.CanExecute(null));
    }

    [Fact]
    public async Task Start_ExerciseDoneBefore_PrefillsFromTheLastSession()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await _ui.App.Data.CompletedSessionAsync(_push, TestApp.StartUtc, new LoggedSet(0, 80, 5), new LoggedSet(0, 82.5, 4));

        await StartAsync(_push);

        Assert.True(Workout.HasLastSession);
        Assert.Equal("Last: 80×5, 82.5×4", Workout.LastSessionResultsText);
        Assert.Equal(("80", "5"), (Workout.SetInput.WeightInput, Workout.SetInput.RepsInput));
        Assert.True(Workout.SaveSetCommand.CanExecute(null));
    }

    [Fact]
    public async Task Start_WorkoutWithoutExercises_ShowsTheEmptyStateAndLogsNothing()
    {
        await StartAsync(await _ui.App.Data.WorkoutAsync("Freestyle"));
        Workout.SetInput.Fill(80, 5);

        await Workout.SaveSetCommand.ExecuteAsync(null);

        Assert.True(Workout.IsExercisesEmpty);
        Assert.Equal(0, await _ui.App.Database.CountAsync(nameof(WorkoutSet)));
    }

    [Fact]
    public async Task SaveSet_ValidInput_LogsTheSetAndStartsTheRest()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);

        await LogAsync(80, 5);

        var logged = Assert.Single(Workout.CurrentSets);
        Assert.Equal("Set 1, 80 kg, 5 reps", logged.Description);
        Assert.True(Workout.HasSavedSets);
        Assert.Equal("Set 2 of 3", Workout.SetProgressText);
        Assert.Equal([true, false, false], Workout.SetSegments.Select(segment => segment.IsFilled));
        Assert.Equal(("80", "5"), (Workout.SetInput.WeightInput, Workout.SetInput.RepsInput));
        Assert.True(Workout.Rest.IsResting);
        Assert.Equal("01:30", Workout.Rest.TimerText);
        _ui.Haptics.Received(1).Click();
        var stored = Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id));
        Assert.Equal((80.0, 5, 1), (stored.Weight, stored.Reps, stored.SetNumber));
    }

    [Fact]
    public async Task SaveSet_ValidInput_SavesTheRestInTheState()
    {
        await StartAsync(_push);

        await LogAsync(80, 5);

        var state = await _ui.App.ActiveState.GetAsync();
        Assert.NotNull(state);
        Assert.Equal((true, Now.AddSeconds(RestSeconds), RestSeconds), (state.IsResting, state.BreakEndTimeUtc, state.RestDurationSeconds));
    }

    [Fact]
    public async Task SaveSet_EveryPlannedSetOfAnExercise_MovesToTheNextOne()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);

        await LogPlannedSetsAsync();

        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal("Exercise 2 of 2", Workout.ExerciseProgressText);
        Assert.Empty(Workout.CurrentSets);
        Assert.Equal((true, false), (Workout.HasPreviousExercise, Workout.HasNextExercise));
        Assert.Equal(1, (await _ui.App.ActiveState.GetAsync())?.CurrentExerciseIndex);
    }

    [Fact]
    public async Task SaveSet_EveryPlannedSetOfTheWorkout_CompletesThePlan()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);
        await LogPlannedSetsAsync();
        Assert.False(Workout.IsPlanComplete);

        await LogPlannedSetsAsync();

        Assert.True(Workout.IsPlanComplete);
        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal("Set 4", Workout.SetProgressText);
        Assert.All(Workout.SetSegments, segment => Assert.True(segment.IsFilled));
    }

    [Fact]
    public async Task SaveSet_ExerciseWithoutRest_StartsNoRest()
    {
        var plan = TestDataBuilder.Plan(_bench);
        plan.BreakTimeInSeconds = 0;
        var noRest = new Workout { Name = "No Rest" };
        await _ui.App.Workouts.SaveWithExercisesAsync(noRest, [plan]);
        await StartAsync(noRest);

        await LogAsync(80, 5);

        Assert.False(Workout.Rest.IsResting);
        Assert.False((await _ui.App.ActiveState.GetAsync())?.IsResting);
    }

    [Theory]
    [InlineData("80", "5", true)]
    [InlineData("82,5", "5", true)]
    [InlineData("80.3", "5", false)]
    [InlineData("abc", "5", false)]
    [InlineData("80", "0", false)]
    [InlineData("", "", false)]
    public async Task SaveSetCommand_Input_CanExecuteOnlyWhenValid(string weight, string reps, bool expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);

        Workout.SetInput.WeightInput = weight;
        Workout.SetInput.RepsInput = reps;

        Assert.Equal(expected, Workout.SaveSetCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveSetCommand_InputBecomesValid_RaisesCanExecuteChanged()
    {
        await StartAsync(_push);
        var raised = 0;
        Workout.SaveSetCommand.CanExecuteChanged += (_, _) => raised++;

        Workout.SetInput.WeightInput = "80";
        Workout.SetInput.RepsInput = "5";

        Assert.True(raised > 0);
        Assert.True(Workout.SaveSetCommand.CanExecute(null));
    }

    [Fact]
    public async Task UndoLastSet_LoggedSets_RemovesTheLastOne()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);
        await LogAsync(80, 5);
        await LogAsync(85, 5);

        await Workout.UndoLastSetCommand.ExecuteAsync(null);

        Assert.Equal("Set 1, 80 kg, 5 reps", Assert.Single(Workout.CurrentSets).Description);
        Assert.Equal("Set 2 of 3", Workout.SetProgressText);
        Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id));
    }

    [Fact]
    public async Task NextAndPreviousExercise_MoveWithinTheWorkoutAndSaveThePosition()
    {
        await StartAsync(_push);

        await Workout.NextExerciseCommand.ExecuteAsync(null);
        await Workout.NextExerciseCommand.ExecuteAsync(null);

        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal(1, (await _ui.App.ActiveState.GetAsync())?.CurrentExerciseIndex);
        await Workout.PreviousExerciseCommand.ExecuteAsync(null);
        await Workout.PreviousExerciseCommand.ExecuteAsync(null);
        Assert.Equal(BenchPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal(0, (await _ui.App.ActiveState.GetAsync())?.CurrentExerciseIndex);
    }

    [Fact]
    public async Task SetActionsEdit_LoggedSet_UpdatesTheStoredSetAndTheList()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);
        await LogAsync(80, 5);
        var logged = Workout.CurrentSets[0].Set;

        Workout.SetActions.ShowCommand.Execute(logged);
        Assert.Equal(("Set 1", "80 kg × 5"), (Workout.SetActions.ActionSetTitle, Workout.SetActions.ActionSetSubtitle));
        await Workout.SetActions.EditCommand.ExecuteAsync(null);
        Assert.Equal((false, true, "Edit set"), (Workout.SetActions.IsActionSheetOpen, Workout.SetActions.IsEditorOpen, Workout.SetActions.EditorTitle));
        Workout.SetActions.Editor.WeightInput = "85";
        await Workout.SetActions.SaveEditorCommand.ExecuteAsync(null);

        Assert.False(Workout.SetActions.IsEditorOpen);
        Assert.Equal("Set 1, 85 kg, 5 reps", Assert.Single(Workout.CurrentSets).Description);
        Assert.Equal(85, Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id)).Weight);
    }

    [Fact]
    public async Task SetActionsDelete_Confirmed_DeletesTheSetAndRenumbersTheRest()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);
        await LogAsync(80, 5);
        await LogAsync(85, 5);
        _ui.AnswerConfirmations(true);

        Workout.SetActions.ShowCommand.Execute(Workout.CurrentSets[0].Set);
        await Workout.SetActions.DeleteCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleDeleteSet, AppResources.BodyDeleteSetConfirmation, AppResources.ButtonDelete, AppResources.ButtonCancel);
        Assert.Equal("Set 1, 85 kg, 5 reps", Assert.Single(Workout.CurrentSets).Description);
        Assert.Equal("Set 2 of 3", Workout.SetProgressText);
        Assert.Equal(1, Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id)).SetNumber);
    }

    [Fact]
    public async Task SetActionsDelete_Cancelled_KeepsTheSet()
    {
        await StartAsync(_push);
        await LogAsync(80, 5);
        _ui.AnswerConfirmations(false);

        Workout.SetActions.ShowCommand.Execute(Workout.CurrentSets[0].Set);
        await Workout.SetActions.DeleteCommand.ExecuteAsync(null);

        Assert.Single(Workout.CurrentSets);
        Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id));
        Assert.False(Workout.SetActions.IsActionSheetOpen);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task FinishWhileConfirming_OtherCommands_AreRefused()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await StartAsync(_push);
        await LogAsync(80, 5);
        var answer = _ui.HoldConfirmations();
        var canShowChanged = 0;
        Workout.SetActions.ShowCommand.CanExecuteChanged += (_, _) => canShowChanged++;

        var finishing = Workout.FinishWorkoutCommand.ExecuteAsync(null);
        Workout.SetInput.Fill(90, 5);
        await Workout.SaveSetCommand.ExecuteAsync(null);
        await Workout.NextExerciseCommand.ExecuteAsync(null);

        Assert.True(Workout.IsBusy);
        Assert.False(Workout.FinishWorkoutCommand.CanExecute(null));
        Assert.False(Workout.SetActions.ShowCommand.CanExecute(Workout.CurrentSets[0].Set));
        Assert.True(canShowChanged > 0);
        Assert.Equal(BenchPress, Workout.CurrentExercise.ExerciseName);
        Assert.Single(await _ui.App.Sets.GetForSessionExerciseAsync(Workout.CurrentExercise.Id));
        answer.SetResult(false);
        await finishing.WaitAsync(cancellation);
        Assert.False(Workout.IsBusy);
        Assert.True(Workout.SetActions.ShowCommand.CanExecute(Workout.CurrentSets[0].Set));
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task SaveSet_SecondTapWhileAdvancing_LogsNothing()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await StartAsync(_push);
        await LogAsync(80, 5);
        await LogAsync(80, 5);
        var benchId = Workout.CurrentExercise.Id;
        _ui.Clock.HoldPauses();

        var third = Workout.SaveSetCommand.ExecuteAsync(null);
        await _ui.Clock.PauseStarted.WaitAsync(cancellation);
        Assert.False(Workout.SaveSetCommand.CanExecute(null));
        await Workout.SaveSetCommand.ExecuteAsync(null);

        _ui.Clock.ReleasePauses();
        await third.WaitAsync(cancellation);
        Assert.Equal(TestDataBuilder.PlannedSets, (await _ui.App.Sets.GetForSessionExerciseAsync(benchId)).Count);
        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
    }

    [Fact]
    public async Task Finish_ConfirmedWithSets_ClosesTheSessionAndShowsTheSummary()
    {
        await StartAsync(_push);
        await LogAsync(80, 5);
        _ui.AnswerConfirmations(true);

        await Workout.FinishWorkoutCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleFinishWorkout, AppResources.BodyFinishWorkoutConfirmation, AppResources.ButtonFinish, AppResources.ButtonCancel);
        Assert.False(Workout.IsWorkoutActive);
        Assert.True(Workout.Summary.IsVisible);
        Assert.False(Workout.Rest.IsResting);
        _ui.WorkoutTicker.Received(1).Stop();
        _ui.AudioCues.Received(1).Stop();
        Assert.Null(await _ui.App.ActiveState.GetAsync());
        Assert.Single(await _ui.App.History.GetWorkoutHistoryAsync(_push.Id));
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    [Fact]
    public async Task Finish_ConfirmedWithoutSets_DiscardsTheSessionAndGoesBack()
    {
        await StartAsync(_push);
        _ui.AnswerConfirmations(true);

        await Workout.FinishWorkoutCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleFinishWorkout, AppResources.BodyFinishUnsavedWorkoutConfirmation, AppResources.ButtonFinish, AppResources.ButtonCancel);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
        Assert.False(Workout.IsWorkoutActive);
        Assert.False(Workout.Summary.IsVisible);
        Assert.Equal(0, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
        Assert.Null(await _ui.App.ActiveState.GetAsync());
    }

    [Fact]
    public async Task Finish_DiscardCancelled_KeepsTheWorkoutRunning()
    {
        await StartAsync(_push);
        _ui.AnswerConfirmations(false);

        await Workout.FinishWorkoutCommand.ExecuteAsync(null);

        Assert.True(Workout.IsWorkoutActive);
        Assert.NotNull(await _ui.App.ActiveState.GetAsync());
        Assert.Equal(1, await _ui.App.Database.CountAsync(nameof(WorkoutSession)));
        _ui.WorkoutTicker.DidNotReceive().Stop();
    }

    [Fact]
    public async Task Finish_CancelledWithSets_KeepsTheSessionOpen()
    {
        await StartAsync(_push);
        await LogAsync(80, 5);
        _ui.AnswerConfirmations(false);

        await Workout.FinishWorkoutCommand.ExecuteAsync(null);

        Assert.True(Workout.IsWorkoutActive);
        Assert.True(Workout.Rest.IsResting);
        Assert.False(Workout.Summary.IsVisible);
        Assert.Empty(await _ui.App.History.GetWorkoutHistoryAsync(_push.Id));
    }

    [Fact]
    public async Task LoadState_SavedSession_ResumesWhereItWas()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var started = await _ui.App.Data.StartAsync(_push);
        await _ui.App.Data.LogSetAsync(started.Exercises[1], 40, 8);
        await _ui.App.Data.SaveActiveStateAsync(started, exerciseIndex: 1);
        _ui.Clock.Advance(TimeSpan.FromMinutes(10));

        await Workout.LoadStateAsync();
        _ui.Tick();

        Assert.True(Workout.IsStateRestored);
        Assert.True(Workout.IsWorkoutActive);
        Assert.Equal(PushDay, Workout.WorkoutTitle);
        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
        Assert.Equal("Set 1, 40 kg, 8 reps", Assert.Single(Workout.CurrentSets).Description);
        Assert.Equal("10:00", Workout.TimerText);
        Assert.False(Workout.CanAddItems);
    }

    [Fact]
    public async Task LoadState_KilledDuringARest_ResumesTheCountdown()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var started = await _ui.App.Data.StartAsync(_push);
        await _ui.App.ActiveSessions.SaveStateAsync(new ActiveWorkoutState
        {
            SessionId = started.SessionId,
            StartTimeUtc = Now,
            IsResting = true,
            BreakEndTimeUtc = Now.AddSeconds(RestSeconds),
            RestDurationSeconds = RestSeconds
        });
        _ui.Clock.Advance(TimeSpan.FromSeconds(30));

        await Workout.LoadStateAsync();

        Assert.True(Workout.Rest.IsResting);
        Assert.Equal(("01:00", "of 90 s"), (Workout.Rest.TimerText, Workout.Rest.TotalText));
    }

    [Fact]
    public async Task LoadState_NothingSaved_OnlyAllowsAddingItems()
    {
        var changed = PropertyChanges.Record(Workout);

        await Workout.LoadStateAsync();

        Assert.Equal((true, false, true), (Workout.IsStateRestored, Workout.IsWorkoutActive, Workout.CanAddItems));
        Assert.Contains(nameof(ActiveWorkoutViewModel.CanAddItems), changed);
        _ui.WorkoutTicker.DidNotReceive().Start();
    }

    [Fact]
    public async Task IsWorkoutActive_Changes_RaiseTheBannerAndAddButtonState()
    {
        await Workout.LoadStateAsync();
        var changed = PropertyChanges.Record(Workout);

        await StartAsync(_push);

        Assert.Contains(nameof(ActiveWorkoutViewModel.IsBannerVisible), changed);
        Assert.Contains(nameof(ActiveWorkoutViewModel.CanAddItems), changed);
        Assert.Equal((true, false), (Workout.IsBannerVisible, Workout.CanAddItems));
        Workout.IsOnActiveWorkoutPage = true;
        Assert.False(Workout.IsBannerVisible);
    }

    [Fact]
    public async Task ResumeWorkout_TabChangesDiscarded_OpensTheWorkoutOnItsTab()
    {
        await StartAsync(_push);

        await Workout.ResumeWorkoutCommand.ExecuteAsync(null);

        await _ui.NavigationStack.Received(1).ConfirmDiscardingChangesOnTabAsync(NavigationRoutes.WorkoutTab);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.ActiveWorkoutOnWorkoutTab));
    }

    [Fact]
    public async Task ResumeWorkout_TabChangesKept_StaysPut()
    {
        await StartAsync(_push);
        _ui.NavigationStack.ConfirmDiscardingChangesOnTabAsync(NavigationRoutes.WorkoutTab).Returns(false);

        await Workout.ResumeWorkoutCommand.ExecuteAsync(null);

        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    [Fact]
    public async Task Tick_DuringTheWorkout_ShowsTheElapsedTime()
    {
        await StartAsync(_push);
        _ui.Clock.Advance(new TimeSpan(1, 5, 0));

        _ui.Tick();

        Assert.Equal("1:05:00", Workout.TimerText);
    }

    [Fact]
    public async Task CatalogChangesDuringTheWorkout_LeaveTheSessionSnapshotAlone()
    {
        await StartAsync(_push);

        await _ui.App.Catalog.UpdateAsync(new Exercise { Id = _bench.Id, Name = "Flat Bench", TargetMuscleGroup = MuscleGroup.Chest });
        await _ui.App.Catalog.DeleteAsync(_press.Id);
        await _ui.SettleAsync();
        await Workout.NextExerciseCommand.ExecuteAsync(null);
        await LogAsync(40, 8);

        Assert.Equal([BenchPress, OverheadPress], Workout.Exercises.Select(exercise => exercise.ExerciseName));
        Assert.Equal(OverheadPress, Workout.CurrentExercise.ExerciseName);
        Assert.Single(Workout.CurrentSets);
    }

    [Fact]
    public async Task CurrentVolumeText_EnglishUs_SumsTheLoggedSets()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await StartAsync(_push);
        var changed = PropertyChanges.Record(Workout);

        await LogAsync(100, 12);
        await LogAsync(82.5, 8);

        Assert.Equal("1,860 kg", Workout.CurrentVolumeText);
        Assert.Contains(nameof(ActiveWorkoutViewModel.CurrentVolumeText), changed);
    }

    [Fact]
    public async Task Displays_Polish_UsePolishFormatsAndPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);
        await StartAsync(_push);

        await LogAsync(100, 12);
        await LogAsync(82.5, 8);

        Assert.Equal($"1{Cultures.PolishGroupSeparator}860 kg", Workout.CurrentVolumeText);
        Assert.Equal(["Seria 1, 100 kg, 12 powtórzeń", "Seria 2, 82,5 kg, 8 powtórzeń"], Workout.CurrentSets.Select(item => item.Description));
        Assert.Equal("Seria 3 z 3 · Cel: 8 powtórzeń · RPE 7", Workout.ProgressCaption);
        Assert.Equal("z 90 s", Workout.Rest.TotalText);
        Assert.Equal("82,5", Workout.SetInput.WeightInput);
    }

    [Fact]
    public async Task LanguageChange_DuringTheWorkout_ReformatsEveryText()
    {
        using var defaults = new DefaultCultureScope();
        _ui.Localization.RestoreSavedLanguage();
        await StartAsync(_push);
        await LogAsync(82.5, 5);
        var changed = PropertyChanges.Record(Workout);

        _ui.Localization.ChangeLanguage(CultureInfo.GetCultureInfo(Cultures.PolishLanguage));

        Assert.Equal("Seria 1, 82,5 kg, 5 powtórzeń", Assert.Single(Workout.CurrentSets).Description);
        Assert.Equal("Ćwiczenie 1 z 2", Workout.ExerciseProgressText);
        Assert.Equal("Seria 2 z 3", Workout.SetProgressText);
        Assert.Equal("Pierwszy raz", Workout.LastSessionResultsText);
        Assert.Equal("z 90 s", Workout.Rest.TotalText);
        Assert.Equal("82,5", Workout.SetInput.WeightInput);
        Assert.Contains(nameof(ActiveWorkoutViewModel.TargetText), changed);
        Assert.Contains(nameof(ActiveWorkoutViewModel.ProgressCaption), changed);
    }

    [Fact]
    public async Task ViewCompletedHistory_AfterFinishing_OpensThisWorkoutsHistory()
    {
        await FinishWithOneSetAsync();

        await Workout.ViewCompletedHistoryCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(
            Route.Is($"{NavigationRoutes.GoBack}/{NavigationRoutes.WorkoutHistory}"),
            Arg.Is<IDictionary<string, object>>(query => (int)query[QueryKeys.WorkoutId] == _push.Id && (string)query[QueryKeys.WorkoutName] == PushDay));
        Assert.False(Workout.Summary.IsVisible);
    }

    [Fact]
    public async Task NavigateBack_SummaryShown_LeavesAndClearsIt()
    {
        await FinishWithOneSetAsync();

        await Workout.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
        Assert.False(Workout.Summary.IsVisible);
    }

    [Fact]
    public async Task NavigateBack_SetSheetOpen_OnlyClosesTheSheet()
    {
        await StartAsync(_push);
        await LogAsync(80, 5);
        Workout.SetActions.ShowCommand.Execute(Workout.CurrentSets[0].Set);

        await Workout.NavigateBackCommand.ExecuteAsync(null);

        Assert.False(Workout.SetActions.IsActionSheetOpen);
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    private void Open(Workout workout) =>
        Workout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = workout });

    private async Task StartAsync(Workout workout)
    {
        Open(workout);
        await _ui.SettleAsync();
    }

    private async Task LogAsync(double weight, int reps)
    {
        Workout.SetInput.Fill(weight, reps);
        await Workout.SaveSetCommand.ExecuteAsync(null);
    }

    private async Task LogPlannedSetsAsync()
    {
        for (var set = 0; set < TestDataBuilder.PlannedSets; set++)
        {
            await LogAsync(80, 5);
        }
    }

    private async Task FinishWithOneSetAsync()
    {
        await StartAsync(_push);
        await LogAsync(80, 5);
        _ui.AnswerConfirmations(true);
        await Workout.FinishWorkoutCommand.ExecuteAsync(null);
    }

}
