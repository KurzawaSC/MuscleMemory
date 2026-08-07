using MuscleMemory.Views;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AddEditWorkoutPage), typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(ExerciseHistoryPage), typeof(ExerciseHistoryPage));
        Routing.RegisterRoute(nameof(WorkoutHistoryPage), typeof(WorkoutHistoryPage));
    }

    public void UpdateTabBarTheme(AppTheme theme)
    {
        bool isDark = theme == AppTheme.Dark || 
            (theme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark);

        Shell.SetTabBarBackgroundColor(this, isDark ? Color.FromArgb("#121212") : Color.FromArgb("#FFFFFF"));
        Shell.SetTabBarUnselectedColor(this, isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#666666"));
        Shell.SetTabBarForegroundColor(this, isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"));
        Shell.SetTabBarTitleColor(this, Color.FromArgb("#FF4040"));
    }
}