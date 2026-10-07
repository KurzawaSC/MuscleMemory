using MuscleMemory.Views;

namespace MuscleMemory.Constants;

public static class NavigationRoutes
{
    public const string GoBack = "..";
    public const string ExerciseTab = nameof(ExerciseListPage);
    public const string WorkoutTab = nameof(WorkoutListPage);
    public const string SettingsTab = nameof(SettingsPage);
    public const string AddEditWorkout = nameof(AddEditWorkoutPage);
    public const string ActiveWorkout = nameof(ActiveWorkoutPage);
    public const string ExerciseHistory = nameof(ExerciseHistoryPage);
    public const string WorkoutHistory = nameof(WorkoutHistoryPage);
    public const string ActiveWorkoutOnWorkoutTab = $"//{WorkoutTab}/{ActiveWorkout}";
}
