using MuscleMemory.Constants;
using MuscleMemory.Diagnostics;
using MuscleMemory.Services;
using MuscleMemory.ViewModels;
using MuscleMemory.Views;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell(ActiveWorkoutViewModel activeWorkoutViewModel, IStatusBarService statusBarService)
    {
        InitializeComponent();

        Routing.RegisterRoute(NavigationRoutes.AddEditWorkout, typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(NavigationRoutes.ActiveWorkout, typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(NavigationRoutes.ExerciseHistory, typeof(ExerciseHistoryPage));
        Routing.RegisterRoute(NavigationRoutes.WorkoutHistory, typeof(WorkoutHistoryPage));

        activeWorkoutViewModel.TrackCurrentPage(this);
        statusBarService.TrackNavigation(this);
        AppLog.LogFailures(activeWorkoutViewModel.LoadStateAsync());
    }
}