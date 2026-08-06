using MuscleMemory.Views;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AddEditWorkoutPage), typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(RestTimerPage), typeof(RestTimerPage));
        Routing.RegisterRoute(nameof(WorkoutSummaryPage), typeof(WorkoutSummaryPage));
        Routing.RegisterRoute(nameof(ExerciseHistoryPage), typeof(ExerciseHistoryPage));
        Routing.RegisterRoute(nameof(WorkoutHistoryPage), typeof(WorkoutHistoryPage));
    }
}