using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

[Collection(nameof(DefaultCultureCollection))]
public sealed class ExerciseListViewModelTests : IAsyncLifetime
{
    private const string BenchPress = "Bench Press";
    private const string InclinePress = "Incline Dumbbell Press";
    private const string BarbellRow = "Barbell Row";
    private const string OverheadPress = "Overhead Press";

    private readonly TestViewModels _ui = new();
    private ExerciseListViewModel _list = null!;

    public ValueTask InitializeAsync()
    {
        _list = _ui.CreateExerciseList();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void ListState_BeforeTheFirstLoad_ClaimsNeitherItemsNorEmpty() =>
        Assert.Equal((false, false), (_list.ListState.IsEmpty, _list.ListState.HasItems));

    [Fact]
    public async Task Load_NoExercises_IsEmpty()
    {
        await _list.LoadExercisesCommand.ExecuteAsync(null);

        Assert.Equal((true, false, false), (_list.ListState.IsEmpty, _list.ListState.HasItems, _list.HasNoMatches));
        Assert.Empty(_list.Exercises);
    }

    [Fact]
    public async Task Load_Exercises_ListsThemInCreationOrderWithTheirDetails()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await _list.LoadExercisesCommand.ExecuteAsync(null);

        Assert.Equal((false, true), (_list.ListState.IsEmpty, _list.ListState.HasItems));
        Assert.Equal([BenchPress, InclinePress, BarbellRow, OverheadPress], _list.Exercises.Select(item => item.Exercise.Name));
        Assert.Equal("Chest · Barbell", _list.Exercises[0].DetailText);
        Assert.Equal(["All", "Chest", "Back", "Shoulders"], _list.Filter.Filters.Select(filter => filter.Label));
    }

    [Fact]
    public async Task Filter_MuscleGroupAndSearch_CombineAndReuseTheLoadedItems()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        var incline = _list.Exercises[1];

        _list.Filter.SelectedFilter = _list.Filter.Filters.Single(filter => filter.MuscleGroup == MuscleGroup.Chest);
        Assert.Equal([BenchPress, InclinePress], _list.Exercises.Select(item => item.Exercise.Name));

        _list.Filter.SearchText = "  INCLINE ";
        Assert.Same(incline, Assert.Single(_list.Exercises));
        Assert.False(_list.HasNoMatches);
    }

    [Fact]
    public async Task Filter_NothingMatches_ShowsNoMatchesUntilCleared()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);

        _list.Filter.SearchText = "Squat";
        Assert.Equal((true, true), (_list.HasNoMatches, _list.ListState.HasItems));
        Assert.Empty(_list.Exercises);

        _list.Filter.SearchText = string.Empty;
        Assert.False(_list.HasNoMatches);
        Assert.Equal(4, _list.Exercises.Count);
    }

    [Fact]
    public async Task ExerciseAddedElsewhere_Notified_ReloadsTheList()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);

        await _ui.App.Data.ExerciseAsync("Deadlift", MuscleGroup.Back);
        await _ui.SettleAsync();

        Assert.Equal("Deadlift", _list.Exercises[^1].Exercise.Name);
    }

    [Fact]
    public async Task WorkoutsOnlyChange_Notified_DoesNotReload()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        await _ui.App.Exercises.AddAsync(new Exercise { Name = "Deadlift" });

        _ui.App.DataChanges.Notify(DataArea.Workouts);
        await _ui.SettleAsync();

        Assert.Equal(4, _list.Exercises.Count);
        _ui.App.DataChanges.Notify(DataArea.Exercises);
        await _ui.SettleAsync();
        Assert.Equal(5, _list.Exercises.Count);
    }

    [Fact]
    public async Task Reload_SelectedGroupGone_FallsBackToAll()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var exercises = await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _list.Filter.SelectedFilter = _list.Filter.Filters.Single(filter => filter.MuscleGroup == MuscleGroup.Shoulders);

        await _ui.App.Catalog.DeleteAsync(exercises.Single(exercise => exercise.Name == OverheadPress).Id);
        await _ui.SettleAsync();

        Assert.Equal("All", _list.Filter.SelectedFilter.Label);
        Assert.Equal(3, _list.Exercises.Count);
    }

    [Fact]
    public async Task SaveExercise_NewExerciseFromTheForm_StoresItClosesTheFormAndShowsIt()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        await _list.AddExerciseCommand.ExecuteAsync(null);
        Assert.True(_list.IsExerciseFormOpen);
        Assert.False(_list.SaveExerciseCommand.CanExecute(null));

        _list.ExerciseForm.Name = "Face Pull";
        Assert.True(_list.SaveExerciseCommand.CanExecute(null));
        await _list.SaveExerciseCommand.ExecuteAsync(null);
        await _ui.SettleAsync();

        Assert.False(_list.IsExerciseFormOpen);
        Assert.Equal("Face Pull", _list.Exercises[^1].Exercise.Name);
    }

    [Fact]
    public async Task SaveExerciseCommand_DuplicateName_CannotExecute()
    {
        await SeedAsync();
        await _list.AddExerciseCommand.ExecuteAsync(null);

        _list.ExerciseForm.Name = BenchPress.ToUpperInvariant();

        Assert.False(_list.SaveExerciseCommand.CanExecute(null));
    }

    [Fact]
    public async Task CancelExerciseForm_Open_ClosesWithoutSaving()
    {
        await _list.AddExerciseCommand.ExecuteAsync(null);
        _list.ExerciseForm.Name = "Face Pull";

        _list.CancelExerciseFormCommand.Execute(null);

        Assert.False(_list.IsExerciseFormOpen);
        Assert.Empty(await _ui.App.Exercises.GetAllAsync());
    }

    [Fact]
    public async Task LongPressExercise_Item_ClicksAndOpensItsActions()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);

        _list.LongPressExerciseCommand.Execute(_list.Exercises[2]);

        _ui.Haptics.Received(1).Click();
        Assert.True(_list.IsActionSheetOpen);
        Assert.Same(_list.Exercises[2], _list.ActionExercise);
        Assert.True(_list.CloseSheetsCommand.CanExecute(null));
    }

    [Fact]
    public async Task CloseSheets_ActionsOpen_ClosesAndForgetsTheExercise()
    {
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _list.ShowExerciseActionsCommand.Execute(_list.Exercises[0]);

        _list.CloseSheetsCommand.Execute(null);

        Assert.False(_list.IsActionSheetOpen);
        Assert.Null(_list.ActionExercise);
        Assert.False(_list.CloseSheetsCommand.CanExecute(null));
    }

    [Fact]
    public async Task EditActionExercise_Saved_RenamesItEverywhere()
    {
        var exercises = await SeedAsync();
        var push = await _ui.App.Data.WorkoutAsync("Push Day", exercises[0]);
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _list.ShowExerciseActionsCommand.Execute(_list.Exercises[0]);

        await _list.EditActionExerciseCommand.ExecuteAsync(null);
        _list.ExerciseForm.Name = "Flat Bench Press";
        await _list.SaveExerciseCommand.ExecuteAsync(null);
        await _ui.SettleAsync();

        Assert.False(_list.IsActionSheetOpen);
        Assert.Equal("Flat Bench Press", _list.Exercises[0].Exercise.Name);
        Assert.Equal("Flat Bench Press", Assert.Single(await _ui.App.Workouts.GetExercisesAsync(push.Id)).ExerciseName);
    }

    [Fact]
    public async Task ViewActionExerciseHistory_Item_OpensItsHistory()
    {
        var exercises = await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _list.ShowExerciseActionsCommand.Execute(_list.Exercises[0]);

        await _list.ViewActionExerciseHistoryCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(
            Route.Is(NavigationRoutes.ExerciseHistory),
            Arg.Is<IDictionary<string, object>>(query => (int)query[QueryKeys.ExerciseId] == exercises[0].Id && (string)query[QueryKeys.ExerciseName] == BenchPress));
    }

    [Fact]
    public async Task DeleteActionExercise_UsedByWorkoutsConfirmed_DeletesItAndWarnsAboutTheWorkouts()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var exercises = await SeedAsync();
        var push = await _ui.App.Data.WorkoutAsync("Push Day", exercises[0], exercises[3]);
        await _ui.App.Data.WorkoutAsync("Chest Day", exercises[0]);
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _ui.AnswerConfirmations(true);
        _list.ShowExerciseActionsCommand.Execute(_list.Exercises[0]);

        await _list.DeleteActionExerciseCommand.ExecuteAsync(null);
        await _ui.SettleAsync();

        await _ui.Dialogs.Received(1).ConfirmAsync(
            AppResources.TitleDeleteExercise, "Are you sure you want to delete “Bench Press”? It will be removed from 2 workouts.", AppResources.ButtonDelete, AppResources.ButtonCancel);
        Assert.DoesNotContain(_list.Exercises, item => item.Exercise.Name == BenchPress);
        Assert.Equal([OverheadPress], (await _ui.App.Workouts.GetExercisesAsync(push.Id)).Select(exercise => exercise.ExerciseName));
    }

    [Fact]
    public async Task DeleteActionExercise_UnusedCancelled_KeepsIt()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await _list.LoadExercisesCommand.ExecuteAsync(null);
        _ui.AnswerConfirmations(false);
        _list.ShowExerciseActionsCommand.Execute(_list.Exercises[2]);

        await _list.DeleteActionExerciseCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(
            AppResources.TitleDeleteExercise, "Are you sure you want to delete “Barbell Row”?", AppResources.ButtonDelete, AppResources.ButtonCancel);
        Assert.Equal(4, (await _ui.App.Exercises.GetAllAsync()).Count);
        Assert.Equal(4, _list.Exercises.Count);
    }

    [Fact]
    public void ActiveWorkout_Exposed_IsTheSharedInstance() =>
        Assert.Same(_ui.ActiveWorkout, _list.ActiveWorkout);

    [Fact]
    public async Task LanguageChange_FormOpen_ReformatsTheForm()
    {
        using var defaults = new DefaultCultureScope();
        _ui.Localization.RestoreSavedLanguage();
        await _list.AddExerciseCommand.ExecuteAsync(null);

        _ui.Localization.ChangeLanguage(CultureInfo.GetCultureInfo(Cultures.PolishLanguage));

        Assert.Equal("Nowe ćwiczenie", _list.ExerciseForm.Title);
        Assert.Equal("Klatka", _list.ExerciseForm.MuscleGroups[0].Label);
    }

    private async Task<List<Exercise>> SeedAsync()
    {
        List<Exercise> exercises =
        [
            await _ui.App.Data.ExerciseAsync(BenchPress, MuscleGroup.Chest, EquipmentType.Barbell),
            await _ui.App.Data.ExerciseAsync(InclinePress, MuscleGroup.Chest, EquipmentType.Dumbbell),
            await _ui.App.Data.ExerciseAsync(BarbellRow, MuscleGroup.Back, EquipmentType.Barbell),
            await _ui.App.Data.ExerciseAsync(OverheadPress, MuscleGroup.Shoulders, EquipmentType.Barbell)
        ];
        await _ui.SettleAsync();
        return exercises;
    }
}
