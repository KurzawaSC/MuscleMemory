using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
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
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("LilitaOne-Regular.ttf", "LilitaOne");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Rejestracja bazy danych jako Singleton
        builder.Services.AddSingleton<Data.DatabaseContext>();

        // Rejestracja Widoków
        builder.Services.AddSingleton<ExerciseListPage>();
        builder.Services.AddSingleton<WorkoutListPage>();
        builder.Services.AddSingleton<SettingsPage>();

        // Rejestracja ViewModeli
        builder.Services.AddSingleton<ExerciseListViewModel>();
        builder.Services.AddSingleton<WorkoutListViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Rejestracja Kreatora Treningów (Transient = za każdym wejściem tworzy się nowy, czysty obiekt)
        builder.Services.AddTransient<AddEditWorkoutPage>();
        builder.Services.AddTransient<AddEditWorkoutViewModel>();

        return builder.Build();
    }
}