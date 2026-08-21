using MuscleMemory.Services;
using MuscleMemory.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell(ActiveWorkoutViewModel activeWorkoutViewModel, IStatusBarService statusBarService)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AddEditWorkoutPage), typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(ExerciseHistoryPage), typeof(ExerciseHistoryPage));
        Routing.RegisterRoute(nameof(WorkoutHistoryPage), typeof(WorkoutHistoryPage));

        activeWorkoutViewModel.TrackCurrentPage(this);
        statusBarService.TrackNavigation(this);
        _ = activeWorkoutViewModel.LoadStateAsync();
    }
}