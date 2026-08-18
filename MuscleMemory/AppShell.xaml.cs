using MuscleMemory.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory;

public partial class AppShell : Shell
{
    public AppShell(ActiveWorkoutViewModel activeWorkoutViewModel)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(AddEditWorkoutPage), typeof(AddEditWorkoutPage));
        Routing.RegisterRoute(nameof(ActiveWorkoutPage), typeof(ActiveWorkoutPage));
        Routing.RegisterRoute(nameof(ExerciseHistoryPage), typeof(ExerciseHistoryPage));
        Routing.RegisterRoute(nameof(WorkoutHistoryPage), typeof(WorkoutHistoryPage));
        
        _ = activeWorkoutViewModel.LoadStateAsync();
    }

    public void UpdateTabBarTheme(AppTheme theme)
    {
        bool isDark = theme == AppTheme.Dark || 
            (theme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark);

        Shell.SetTabBarBackgroundColor(this, isDark ? Color.FromArgb("#1C1C1E") : Color.FromArgb("#FFFFFF"));
        Shell.SetTabBarUnselectedColor(this, isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#666666"));
        Shell.SetTabBarForegroundColor(this, isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"));
        Shell.SetTabBarTitleColor(this, isDark ? Color.FromArgb("#D32F2F") : Color.FromArgb("#FF4040"));
    }
}