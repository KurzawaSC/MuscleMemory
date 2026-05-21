using MuscleMemory.Views;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Rejestracja tras dla podstron, do których przechodzimy programowo.
        // Dzięki temu będziesz mógł używać czytelnych stringów do nawigacji.

        Routing.RegisterRoute(nameof(AddEditWorkoutPage), typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(RestTimerPage), typeof(RestTimerPage));
        Routing.RegisterRoute(nameof(WorkoutSummaryPage), typeof(WorkoutSummaryPage));
        Routing.RegisterRoute(nameof(ExerciseHistoryPage), typeof(ExerciseHistoryPage));
    }
}