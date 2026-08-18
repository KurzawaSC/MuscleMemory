using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using MuscleMemory.Views;
using MuscleMemory.ViewModels;

namespace MuscleMemory;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("LilitaOne-Regular.ttf", "LilitaOne");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<Data.DatabaseContext>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<ExerciseListPage>();
        builder.Services.AddSingleton<WorkoutListPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<ExerciseListViewModel>();
        builder.Services.AddSingleton<WorkoutListViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddTransient<AddEditWorkoutPage>();
        builder.Services.AddTransient<AddEditWorkoutViewModel>();
        builder.Services.AddSingleton<ActiveWorkoutViewModel>();
        builder.Services.AddSingleton<ActiveWorkoutPage>();
        builder.Services.AddTransient<ExerciseHistoryViewModel>();
        builder.Services.AddTransient<ExerciseHistoryPage>();
        builder.Services.AddTransient<WorkoutHistoryViewModel>();
        builder.Services.AddTransient<WorkoutHistoryPage>();
        builder.Services.AddTransientPopup<ConfigureExercisePopup, ConfigureExerciseViewModel>();
        builder.Services.AddTransientPopup<AddExercisePopup, AddEditExerciseViewModel>();

        return builder.Build();
    }
}