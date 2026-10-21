using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class AddEditWorkoutViewModelTests : IAsyncLifetime
{
    private const string EditorLocation = "//" + NavigationRoutes.WorkoutTab + "/" + NavigationRoutes.AddEditWorkout;
    private const string WorkoutListLocation = "//" + NavigationRoutes.WorkoutTab;
    private const string SettingsLocation = "//" + NavigationRoutes.SettingsTab;
    private const string PushedLocation = EditorLocation + "/" + NavigationRoutes.ExerciseHistory;
    private const string PushDay = "Push Day";
    private const string BenchPress = "Bench Press";
    private const string OverheadPress = "Overhead Press";

    private readonly TestViewModels _ui = new();
    private AddEditWorkoutViewModel _editor = null!;
    private Exercise _bench = null!;
    private Exercise _press = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest);
        _press = await _ui.App.Data.ExerciseAsync(OverheadPress, MuscleGroup.Shoulders);
        _editor = _ui.CreateWorkoutEditor();
        _ui.Navigation.CurrentState.Returns(new ShellNavigationState(WorkoutListLocation));
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void New_Always_IsEmptyAndCannotBeSaved()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var editor = _ui.CreateWorkoutEditor();

        Assert.Equal("New workout", editor.HeaderTitle);
        Assert.True(editor.IsEmptyHintVisible);
        Assert.Equal(("exercises", "sets"), (editor.ExerciseCountCaption, editor.TotalSetsCaption));
        Assert.False(editor.SaveWorkoutCommand.CanExecute(null));
        Assert.False(editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task AddExercise_PickedAndConfirmed_JoinsTheWorkout()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        await AddAsync(_bench, sets: 4);

        var added = Assert.Single(_editor.Exercises);
        Assert.Equal((BenchPress, 4, "4 × 10"), (added.ExerciseName, added.Sets, added.SetsByRepsText));
        Assert.Equal((1, 4, "exercise", "sets"), (_editor.ExerciseCount, _editor.TotalSets, _editor.ExerciseCountCaption, _editor.TotalSetsCaption));
        Assert.True(_editor.HasUnsavedChanges);
        Assert.False(_editor.IsConfigurationOpen);
    }

    [Fact]
    public async Task AddExercise_Opened_ShowsThePickerWithEveryExercise()
    {
        await _editor.AddExerciseCommand.ExecuteAsync(null);

        Assert.True(_editor.IsExercisePickerOpen);
        Assert.Equal([BenchPress, OverheadPress], _editor.ExercisePicker.Exercises.Select(item => item.Exercise.Name));
    }

    [Fact]
    public async Task PickExercise_Picked_ClosesThePickerAndConfiguresTheDefaults()
    {
        await _editor.AddExerciseCommand.ExecuteAsync(null);

        await _editor.PickExerciseCommand.ExecuteAsync(_press);

        Assert.False(_editor.IsExercisePickerOpen);
        Assert.True(_editor.IsConfigurationOpen);
        Assert.Equal((OverheadPress, false), (_editor.ExerciseConfiguration.ExerciseName, _editor.ExerciseConfiguration.IsEditing));
        Assert.Equal(DomainDefaults.Sets, _editor.ExerciseConfiguration.Sets);
    }

    [Fact]
    public async Task CancelConfiguration_NewExercise_AddsNothing()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);

        _editor.CancelConfigurationCommand.Execute(null);

        Assert.Empty(_editor.Exercises);
        Assert.False(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditExercise_Confirmed_UpdatesTheRowInPlace()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await AddAsync(_bench, sets: 3);
        var row = _editor.Exercises[0];
        var rowChanges = PropertyChanges.Record(row);

        _editor.EditExerciseCommand.Execute(row);
        _editor.ExerciseConfiguration.Sets = 5;
        _editor.ExerciseConfiguration.SelectRpeCommand.Execute(9);
        _editor.ConfirmConfigurationCommand.Execute(null);

        Assert.Same(row, Assert.Single(_editor.Exercises));
        Assert.Equal((5, 9, "RPE 9"), (row.Sets, row.TargetRPE, row.RpeText));
        Assert.Equal(5, _editor.TotalSets);
        Assert.Contains(nameof(WorkoutExerciseItem.SetsByRepsText), rowChanges);
    }

    [Fact]
    public async Task RemoveEditedExercise_Row_RemovesItFromTheWorkout()
    {
        await AddAsync(_bench, sets: 3);
        await AddAsync(_press, sets: 2);

        _editor.EditExerciseCommand.Execute(_editor.Exercises[0]);
        _editor.RemoveEditedExerciseCommand.Execute(null);

        Assert.Equal([OverheadPress], _editor.Exercises.Select(item => item.ExerciseName));
        Assert.Equal((1, 2), (_editor.ExerciseCount, _editor.TotalSets));
        Assert.False(_editor.IsConfigurationOpen);
    }

    [Theory]
    [InlineData("", true, false)]
    [InlineData("   ", true, false)]
    [InlineData(PushDay, false, false)]
    [InlineData(PushDay, true, true)]
    public async Task SaveWorkoutCommand_NameAndExercises_CanExecuteOnlyWithBoth(string name, bool withExercise, bool expected)
    {
        if (withExercise)
        {
            await AddAsync(_bench, sets: 3);
        }

        _editor.WorkoutName = name;

        Assert.Equal(expected, _editor.SaveWorkoutCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveWorkout_New_StoresTheWorkoutAndGoesBack()
    {
        await AddAsync(_bench, sets: 4);
        await AddAsync(_press, sets: 2);
        _editor.WorkoutName = $"  {PushDay} ";

        await _editor.SaveWorkoutCommand.ExecuteAsync(null);

        var stored = Assert.Single(await _ui.App.Workouts.GetAllAsync());
        Assert.Equal(PushDay, stored.Name);
        Assert.Equal(
            [(_bench.Id, 4, 0), (_press.Id, 2, 1)],
            (await _ui.App.Workouts.GetExercisesAsync(stored.Id)).Select(exercise => (exercise.ExerciseId, exercise.Sets, exercise.Order)));
        Assert.False(_editor.HasUnsavedChanges);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
    }

    [Fact]
    public async Task EditExisting_Loaded_ShowsTheWorkoutWithoutUnsavedChanges()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);

        await OpenAsync(push);

        Assert.Equal(("Edit workout", PushDay), (_editor.HeaderTitle, _editor.WorkoutName));
        Assert.Equal([BenchPress, OverheadPress], _editor.Exercises.Select(item => item.ExerciseName));
        Assert.Equal((2, 2 * TestDataBuilder.PlannedSets), (_editor.ExerciseCount, _editor.TotalSets));
        Assert.False(_editor.IsLoading);
        Assert.False(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditExisting_WhileLoading_CannotBeEdited()
    {
        var push = await _ui.App.Data.WorkoutAsync(PushDay, _bench);

        _editor.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.WorkoutToEdit] = push });

        Assert.True(_editor.IsLoading);
        Assert.False(_editor.AddExerciseCommand.CanExecute(null));
        Assert.False(_editor.SaveWorkoutCommand.CanExecute(null));
        await _ui.SettleAsync();
        Assert.True(_editor.AddExerciseCommand.CanExecute(null));
        Assert.True(_editor.SaveWorkoutCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveWorkout_Existing_UpdatesTheStoredPlan()
    {
        var push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);
        await OpenAsync(push);
        _editor.WorkoutName = "Push Heavy";
        _editor.EditExerciseCommand.Execute(_editor.Exercises[1]);
        _editor.RemoveEditedExerciseCommand.Execute(null);

        await _editor.SaveWorkoutCommand.ExecuteAsync(null);

        Assert.Equal("Push Heavy", Assert.Single(await _ui.App.Workouts.GetAllAsync()).Name);
        Assert.Equal([_bench.Id], (await _ui.App.Workouts.GetExercisesAsync(push.Id)).Select(exercise => exercise.ExerciseId));
        Assert.Equal("Push Heavy", push.Name);
    }

    [Fact]
    public async Task CreateExercise_FromThePicker_StoresItAndConfiguresIt()
    {
        await _editor.AddExerciseCommand.ExecuteAsync(null);
        await _editor.CreateExerciseCommand.ExecuteAsync(null);
        Assert.Equal((false, true), (_editor.IsExercisePickerOpen, _editor.IsExerciseFormOpen));
        _editor.ExerciseForm.Name = "Dips";

        await _editor.SaveNewExerciseCommand.ExecuteAsync(null);

        Assert.Contains(await _ui.App.Exercises.GetAllAsync(), exercise => exercise.Name == "Dips");
        Assert.Equal((false, true, "Dips"), (_editor.IsExerciseFormOpen, _editor.IsConfigurationOpen, _editor.ExerciseConfiguration.ExerciseName));
    }

    [Fact]
    public async Task SaveNewExerciseCommand_TakenName_CannotExecute()
    {
        await _editor.CreateExerciseCommand.ExecuteAsync(null);

        _editor.ExerciseForm.Name = BenchPress;

        Assert.False(_editor.SaveNewExerciseCommand.CanExecute(null));
    }

    [Fact]
    public async Task NavigateBack_UnsavedChangesDiscarded_GoesBack()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(true);

        await _editor.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleUnsavedChanges, AppResources.BodyUnsavedChangesConfirmation, AppResources.ButtonDiscard, AppResources.ButtonCancel);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
        Assert.Empty(await _ui.App.Workouts.GetAllAsync());
    }

    [Fact]
    public async Task NavigateBack_UnsavedChangesKept_StaysInTheEditor()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(false);

        await _editor.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
        Assert.True(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task NavigateBack_NoChanges_GoesBackWithoutAsking()
    {
        await _editor.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
    }

    [Fact]
    public async Task NavigateBack_SheetOpen_OnlyClosesTheSheet()
    {
        await _editor.AddExerciseCommand.ExecuteAsync(null);

        await _editor.NavigateBackCommand.ExecuteAsync(null);

        Assert.False(_editor.IsExercisePickerOpen);
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
    }

    [Fact]
    public async Task Guard_LeavingWithUnsavedChangesDiscarded_CancelsThenGoesWhereTheUserWent()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(true);
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);

        var leaving = await NavigateAsync(SettingsLocation, ShellNavigationSource.ShellSectionChanged);

        Assert.True(leaving.Cancelled);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(SettingsLocation));
        Assert.False(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task Guard_PoppingWithUnsavedChangesDiscarded_OnlyGoesBack()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(true);
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);

        var leaving = await NavigateAsync(WorkoutListLocation, ShellNavigationSource.Pop);

        Assert.True(leaving.Cancelled);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.GoBack));
        await _ui.Navigation.DidNotReceive().GoToAsync(Route.Is(WorkoutListLocation));
    }

    [Fact]
    public async Task Guard_LeavingWithUnsavedChangesKept_StaysInTheEditor()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(false);
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);

        var leaving = await NavigateAsync(SettingsLocation, ShellNavigationSource.ShellSectionChanged);

        Assert.True(leaving.Cancelled);
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
        Assert.True(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task Guard_PushAboveTheEditor_IsAllowedWithoutAsking()
    {
        await AddAsync(_bench, sets: 3);
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);

        var pushing = await NavigateAsync(PushedLocation, ShellNavigationSource.Push);

        Assert.False(pushing.Cancelled);
        await _ui.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
    }

    [Fact]
    public async Task Guard_NoUnsavedChanges_LetsTheNavigationThrough()
    {
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);

        var leaving = await NavigateAsync(SettingsLocation, ShellNavigationSource.ShellSectionChanged);

        Assert.False(leaving.Cancelled);
    }

    [Fact]
    public async Task Guard_EditorGoneFromTheStack_StopsGuarding()
    {
        await AddAsync(_bench, sets: 3);
        _editor.StartGuardingUnsavedChangesCommand.Execute(null);
        _ui.NavigationStack.ContainsPageBoundTo(_editor).Returns(false);

        _ui.Navigation.Navigated += Raise.EventWith(_ui.Navigation, new ShellNavigatedEventArgs(
            new ShellNavigationState(EditorLocation), new ShellNavigationState(WorkoutListLocation), ShellNavigationSource.Pop));
        var leaving = await NavigateAsync(SettingsLocation, ShellNavigationSource.ShellSectionChanged);

        Assert.False(leaving.Cancelled);
    }

    [Fact]
    public async Task ConfirmDiscard_AskedByAnotherTab_FollowsTheAnswer()
    {
        await AddAsync(_bench, sets: 3);
        _ui.AnswerConfirmations(false);

        Assert.False(await _editor.ConfirmDiscardAsync());
        Assert.True(_editor.HasUnsavedChanges);
        _ui.AnswerConfirmations(true);
        Assert.True(await _editor.ConfirmDiscardAsync());
        Assert.False(_editor.HasUnsavedChanges);
    }

    [Fact]
    public async Task ConfirmConfigurationCommand_InvalidRepRange_CannotExecuteUntilFixed()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);
        var raised = 0;
        _editor.ConfirmConfigurationCommand.CanExecuteChanged += (_, _) => raised++;

        _editor.ExerciseConfiguration.MinRepsInput = "8";

        Assert.False(_editor.ConfirmConfigurationCommand.CanExecute(null));
        _editor.ExerciseConfiguration.MaxRepsInput = "12";
        Assert.True(_editor.ConfirmConfigurationCommand.CanExecute(null));
        Assert.True(raised >= 2);
    }

    [Fact]
    public async Task ConfirmConfigurationCommand_MinThenMaxTyped_CanExecuteOnlyWhileTheRangeIsValid()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);
        var range = _editor.ExerciseConfiguration;
        List<bool> canConfirm = [];

        range.IsMinRepsFocused = true;
        canConfirm.Add(CanConfirm());
        range.MinRepsInput = "8";
        canConfirm.Add(CanConfirm());
        range.IsMinRepsFocused = false;
        range.IsMaxRepsFocused = true;
        canConfirm.Add(CanConfirm());
        range.MaxRepsInput = "1";
        canConfirm.Add(CanConfirm());
        range.MaxRepsInput = "12";
        canConfirm.Add(CanConfirm());
        range.IsMaxRepsFocused = false;
        canConfirm.Add(CanConfirm());

        Assert.Equal([true, false, false, false, true, true], canConfirm);
    }

    [Fact]
    public async Task ConfirmConfigurationCommand_MaxThenMinTyped_CanExecuteOnlyWhileTheRangeIsValid()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);
        var range = _editor.ExerciseConfiguration;
        List<bool> canConfirm = [];

        range.IsMaxRepsFocused = true;
        range.MaxRepsInput = "1";
        canConfirm.Add(CanConfirm());
        range.MaxRepsInput = "12";
        canConfirm.Add(CanConfirm());
        range.IsMaxRepsFocused = false;
        range.IsMinRepsFocused = true;
        canConfirm.Add(CanConfirm());
        range.MinRepsInput = "8";
        canConfirm.Add(CanConfirm());
        range.IsMinRepsFocused = false;
        canConfirm.Add(CanConfirm());

        Assert.Equal([false, false, false, true, true], canConfirm);
    }

    [Fact]
    public async Task ConfirmConfigurationCommand_UpperBelowLower_CannotExecuteWhetherShownOrNot()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);
        var range = _editor.ExerciseConfiguration;
        range.MinRepsInput = "12";
        range.IsMaxRepsFocused = true;
        List<bool> canConfirm = [];

        range.MaxRepsInput = "8";
        canConfirm.Add(CanConfirm());
        range.IsMaxRepsFocused = false;
        canConfirm.Add(CanConfirm());
        range.IsMaxRepsFocused = true;
        range.MaxRepsInput = "15";
        canConfirm.Add(CanConfirm());

        Assert.Equal([false, false, true], canConfirm);
    }

    [Fact]
    public async Task SaveWorkout_NewExerciseWithRepRange_StoresTheRange()
    {
        await _editor.PickExerciseCommand.ExecuteAsync(_bench);
        _editor.ExerciseConfiguration.MinRepsInput = "8";
        _editor.ExerciseConfiguration.MaxRepsInput = "12";
        _editor.ConfirmConfigurationCommand.Execute(null);
        await AddAsync(_press, sets: 3);
        _editor.WorkoutName = PushDay;

        await _editor.SaveWorkoutCommand.ExecuteAsync(null);

        var stored = Assert.Single(await _ui.App.Workouts.GetAllAsync());
        Assert.Equal<(int?, int?)>(
            [(8, 12), (null, null)],
            (await _ui.App.Workouts.GetExercisesAsync(stored.Id)).Select(exercise => (exercise.MinReps, exercise.MaxReps)));
    }

    [Fact]
    public async Task EditExercise_RepRangeChanged_MarksUnsavedChangesAndSavesIt()
    {
        var push = await _ui.App.Data.PlannedWorkoutAsync(PushDay, TestDataBuilder.Plan(_bench, 8, 12), TestDataBuilder.Plan(_press));
        await OpenAsync(push);

        _editor.EditExerciseCommand.Execute(_editor.Exercises[0]);
        Assert.Equal(("8", "12"), (_editor.ExerciseConfiguration.MinRepsInput, _editor.ExerciseConfiguration.MaxRepsInput));
        _editor.ExerciseConfiguration.MinRepsInput = "6";
        _editor.ExerciseConfiguration.MaxRepsInput = "10";
        _editor.ConfirmConfigurationCommand.Execute(null);

        Assert.True(_editor.HasUnsavedChanges);
        await _editor.SaveWorkoutCommand.ExecuteAsync(null);
        var stored = (await _ui.App.Workouts.GetExercisesAsync(push.Id))[0];
        Assert.Equal((6, 10), (stored.MinReps, stored.MaxReps));
    }

    [Fact]
    public async Task EditExercise_RepRangeCleared_SavesNoRange()
    {
        var push = await _ui.App.Data.PlannedWorkoutAsync(PushDay, TestDataBuilder.Plan(_bench, 8, 12));
        await OpenAsync(push);

        _editor.EditExerciseCommand.Execute(_editor.Exercises[0]);
        _editor.ExerciseConfiguration.MinRepsInput = string.Empty;
        _editor.ExerciseConfiguration.MaxRepsInput = string.Empty;
        _editor.ConfirmConfigurationCommand.Execute(null);
        await _editor.SaveWorkoutCommand.ExecuteAsync(null);

        var stored = Assert.Single(await _ui.App.Workouts.GetExercisesAsync(push.Id));
        Assert.Equal((null, null), (stored.MinReps, stored.MaxReps));
    }

    [Fact]
    public async Task NavigateBack_AfterChangingOnlyTheRange_AsksBeforeDiscarding()
    {
        var push = await _ui.App.Data.PlannedWorkoutAsync(PushDay, TestDataBuilder.Plan(_bench, 8, 12));
        await OpenAsync(push);
        _editor.EditExerciseCommand.Execute(_editor.Exercises[0]);
        _editor.ExerciseConfiguration.MaxRepsInput = "15";
        _editor.ConfirmConfigurationCommand.Execute(null);
        _ui.AnswerConfirmations(false);

        await _editor.NavigateBackCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleUnsavedChanges, AppResources.BodyUnsavedChangesConfirmation, AppResources.ButtonDiscard, AppResources.ButtonCancel);
        await _ui.Navigation.DidNotReceive().GoToAsync(Route.Is(NavigationRoutes.GoBack));
        Assert.Equal(12, (await _ui.App.Workouts.GetExercisesAsync(push.Id))[0].MaxReps);
    }

    private bool CanConfirm() => _editor.ConfirmConfigurationCommand.CanExecute(null);

    private async Task AddAsync(Exercise exercise, int sets)
    {
        await _editor.PickExerciseCommand.ExecuteAsync(exercise);
        _editor.ExerciseConfiguration.Sets = sets;
        _editor.ConfirmConfigurationCommand.Execute(null);
    }

    private async Task OpenAsync(Workout workout)
    {
        _editor.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.WorkoutToEdit] = workout });
        await _ui.SettleAsync();
    }

    private async Task<ShellNavigatingEventArgs> NavigateAsync(string target, ShellNavigationSource source)
    {
        var navigating = new ShellNavigatingEventArgs(new ShellNavigationState(EditorLocation), new ShellNavigationState(target), source, canCancel: true);
        _ui.Navigation.Navigating += Raise.EventWith(_ui.Navigation, navigating);
        await _ui.SettleAsync();
        return navigating;
    }
}
