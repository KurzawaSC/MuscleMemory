using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class WorkoutListViewModelTests : IAsyncLifetime
{
    private const int TimeoutMilliseconds = 10_000;
    private const string PushDay = "Push Day";
    private const string LegDay = "Leg Day";
    private const string Freestyle = "Freestyle";

    private readonly TestViewModels _ui = new();
    private WorkoutListViewModel _list = null!;
    private Exercise _bench = null!;
    private Exercise _press = null!;

    public async ValueTask InitializeAsync()
    {
        _bench = await _ui.App.Data.ExerciseAsync("Bench Press");
        _press = await _ui.App.Data.ExerciseAsync("Overhead Press");
        _list = _ui.CreateWorkoutList();
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void ListState_BeforeTheFirstLoad_ClaimsNeitherItemsNorEmpty() =>
        Assert.Equal((false, false), (_list.ListState.IsEmpty, _list.ListState.HasItems));

    [Fact]
    public async Task Load_NoWorkouts_IsEmpty()
    {
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        Assert.Equal((true, false), (_list.ListState.IsEmpty, _list.ListState.HasItems));
    }

    [Fact]
    public async Task Load_Workouts_ListsThemWithTheirPlans()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();

        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        Assert.Equal([PushDay, LegDay, Freestyle], _list.Workouts.Select(item => item.Workout.Name));
        Assert.Equal(["Bench Press · Overhead Press", "Overhead Press", string.Empty], _list.Workouts.Select(item => item.ExerciseSummary));
        Assert.Equal(["6 sets", "3 sets", "0 sets"], _list.Workouts.Select(item => item.SetsText));
        Assert.Equal([true, true, false], _list.Workouts.Select(item => item.HasExercises));
    }

    [Fact]
    public async Task Load_Polish_CountsSetsWithPolishPlurals()
    {
        using var scope = new CultureScope(Cultures.Polish);
        await SeedAsync();

        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        Assert.Equal(["6 serii", "3 serie", "0 serii"], _list.Workouts.Select(item => item.SetsText));
    }

    [Fact]
    public async Task ExerciseRenamed_Notified_ReloadsTheSummaries()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        await _ui.App.Catalog.UpdateAsync(new Exercise { Id = _bench.Id, Name = "Flat Bench" });
        await _ui.SettleAsync();

        Assert.Equal("Flat Bench · Overhead Press", _list.Workouts[0].ExerciseSummary);
    }

    [Fact]
    public async Task ExercisesOnlyChange_Notified_DoesNotReload()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        await _ui.App.Data.WorkoutAsync("Arm Day", _bench);

        _ui.App.DataChanges.Notify(DataArea.Exercises);
        await _ui.SettleAsync();

        Assert.Equal(3, _list.Workouts.Count);
        _ui.App.DataChanges.Notify(DataArea.Workouts);
        await _ui.SettleAsync();
        Assert.Equal(4, _list.Workouts.Count);
    }

    [Fact]
    public async Task StartWorkoutCommand_WorkoutWithoutExercises_CannotExecute()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        Assert.Equal([true, true, false], _list.Workouts.Select(item => _list.StartWorkoutCommand.CanExecute(item)));
    }

    [Fact]
    public async Task StartWorkout_NoneActive_OpensTheActiveWorkoutWithIt()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        await _list.StartWorkoutCommand.ExecuteAsync(_list.Workouts[0]);

        await _ui.Navigation.Received(1).GoToAsync(
            Route.Is(NavigationRoutes.ActiveWorkout),
            Arg.Is<IDictionary<string, object>>(query => ReferenceEquals(query[QueryKeys.Workout], _list.Workouts[0].Workout)));
    }

    [Fact]
    public async Task StartWorkout_AnotherActiveAndResumeChosen_ReturnsToIt()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        await StartActiveAsync(_list.Workouts[1].Workout);
        _ui.AnswerConfirmations(true);

        await _list.StartWorkoutCommand.ExecuteAsync(_list.Workouts[0]);

        await _ui.Dialogs.Received(1).ConfirmAsync(
            AppResources.TitleHoldOn, "“Leg Day” is still in progress. Finish it before starting another workout.", AppResources.ButtonResume, AppResources.ButtonCancel);
        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.ActiveWorkoutOnWorkoutTab));
        await _ui.Navigation.DidNotReceive().GoToAsync(Route.Is(NavigationRoutes.ActiveWorkout), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task StartWorkout_AnotherActiveAndCancelled_StaysOnTheList()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        await StartActiveAsync(_list.Workouts[1].Workout);
        _ui.AnswerConfirmations(false);

        await _list.StartWorkoutCommand.ExecuteAsync(_list.Workouts[0]);

        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!);
        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!, default!);
    }

    [Fact(Timeout = TimeoutMilliseconds)]
    public async Task StartWorkout_WhileTheActiveWorkoutIsBusy_IsIgnored()
    {
        var cancellation = TestContext.Current.CancellationToken;
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        _ui.Clock.HoldPauses();
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = _list.Workouts[1].Workout });
        await _ui.Clock.PauseStarted.WaitAsync(cancellation);

        await _list.StartWorkoutCommand.ExecuteAsync(_list.Workouts[0]);

        await _ui.Navigation.DidNotReceiveWithAnyArgs().GoToAsync(default!, default!);
        await _ui.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        _ui.Clock.ReleasePauses();
        await _ui.SettleAsync().WaitAsync(cancellation);
    }

    [Fact]
    public async Task NavigateToAddWorkout_Always_OpensAnEmptyEditor()
    {
        await _list.NavigateToAddWorkoutCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(Route.Is(NavigationRoutes.AddEditWorkout));
    }

    [Fact]
    public async Task LongPressWorkout_Item_ClicksAndOpensItsActions()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);

        _list.LongPressWorkoutCommand.Execute(_list.Workouts[1]);

        _ui.Haptics.Received(1).Click();
        Assert.True(_list.IsActionSheetOpen);
        Assert.Same(_list.Workouts[1], _list.ActionWorkout);
        Assert.True(_list.CloseSheetsCommand.CanExecute(null));
        _list.CloseSheetsCommand.Execute(null);
        Assert.Null(_list.ActionWorkout);
        Assert.False(_list.CloseSheetsCommand.CanExecute(null));
    }

    [Fact]
    public async Task EditActionWorkout_Item_OpensTheEditorForIt()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        var push = _list.Workouts[0];
        _list.ShowWorkoutActionsCommand.Execute(push);

        await _list.EditActionWorkoutCommand.ExecuteAsync(null);

        Assert.False(_list.IsActionSheetOpen);
        await _ui.Navigation.Received(1).GoToAsync(
            Route.Is(NavigationRoutes.AddEditWorkout),
            Arg.Is<IDictionary<string, object>>(query => ReferenceEquals(query[QueryKeys.WorkoutToEdit], push.Workout)));
    }

    [Fact]
    public async Task ViewActionWorkoutHistory_Item_OpensItsHistory()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        var legs = _list.Workouts[1].Workout;
        _list.ShowWorkoutActionsCommand.Execute(_list.Workouts[1]);

        await _list.ViewActionWorkoutHistoryCommand.ExecuteAsync(null);

        await _ui.Navigation.Received(1).GoToAsync(
            Route.Is(NavigationRoutes.WorkoutHistory),
            Arg.Is<IDictionary<string, object>>(query => (int)query[QueryKeys.WorkoutId] == legs.Id && (string)query[QueryKeys.WorkoutName] == LegDay));
    }

    [Fact]
    public async Task DeleteActionWorkout_Confirmed_DeletesThePlanButKeepsItsHistory()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var push = await SeedAsync();
        await _ui.App.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        _ui.AnswerConfirmations(true);
        _list.ShowWorkoutActionsCommand.Execute(_list.Workouts[0]);

        await _list.DeleteActionWorkoutCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(
            AppResources.TitleDeleteWorkout,
            "Are you sure you want to delete “Push Day”? Session history is kept. The plan itself can't be restored.",
            AppResources.ButtonDelete,
            AppResources.ButtonCancel);
        Assert.Equal([LegDay, Freestyle], _list.Workouts.Select(item => item.Workout.Name));
        Assert.DoesNotContain(await _ui.App.Workouts.GetAllAsync(), workout => workout.Id == push.Id);
        Assert.Single(await _ui.App.History.GetWorkoutHistoryAsync(push.Id));
    }

    [Fact]
    public async Task DeleteActionWorkout_Cancelled_KeepsIt()
    {
        await SeedAsync();
        await _list.LoadWorkoutsCommand.ExecuteAsync(null);
        _ui.AnswerConfirmations(false);
        _list.ShowWorkoutActionsCommand.Execute(_list.Workouts[0]);

        await _list.DeleteActionWorkoutCommand.ExecuteAsync(null);

        Assert.Equal(3, _list.Workouts.Count);
        Assert.Equal(3, (await _ui.App.Workouts.GetAllAsync()).Count);
    }

    [Fact]
    public void ActiveWorkout_Exposed_IsTheSharedInstance() =>
        Assert.Same(_ui.ActiveWorkout, _list.ActiveWorkout);

    private async Task<Workout> SeedAsync()
    {
        var push = await _ui.App.Data.WorkoutAsync(PushDay, _bench, _press);
        await _ui.App.Data.WorkoutAsync(LegDay, _press);
        await _ui.App.Data.WorkoutAsync(Freestyle);
        return push;
    }

    private async Task StartActiveAsync(Workout workout)
    {
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = workout });
        await _ui.SettleAsync();
    }
}
