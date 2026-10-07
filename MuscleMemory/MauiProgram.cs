using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Services;
using MuscleMemory.ViewModels;
using MuscleMemory.Views;
using Plugin.Maui.Audio;

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
                fonts.AddFont(FontFamilies.LilitaOneFile, FontFamilies.LilitaOne);
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<Shell, TabBarShellRenderer>();
                BorderlessEntryMapping.Register();
                NumericEntryMapping.Register();
#endif
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton(AudioManager.Current);
        builder.Services.AddSingleton<DatabaseContext>();
        builder.Services.AddSingleton<IExerciseRepository, ExerciseRepository>();
        builder.Services.AddSingleton<IWorkoutRepository, WorkoutRepository>();
        builder.Services.AddSingleton<IWorkoutSessionRepository, WorkoutSessionRepository>();
        builder.Services.AddSingleton<ISessionExerciseRepository, SessionExerciseRepository>();
        builder.Services.AddSingleton<IWorkoutSetRepository, WorkoutSetRepository>();
        builder.Services.AddSingleton<IActiveWorkoutStateRepository, ActiveWorkoutStateRepository>();
        builder.Services.AddSingleton<IWorkoutHistoryQueryService, WorkoutHistoryQueryService>();
        builder.Services.AddSingleton<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        builder.Services.AddSingleton<IExerciseCatalogService, ExerciseCatalogService>();
        builder.Services.AddSingleton<IStatusBarService, StatusBarService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IAudioCueService, AudioCueService>();
        builder.Services.AddSingleton<IWorkoutTimerService, WorkoutTimerService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<IErrorHandler, ErrorHandler>();
        builder.Services.AddSingleton<IWorkoutSummaryService, WorkoutSummaryService>();
        builder.Services.AddSingleton<INavigationStackService, NavigationStackService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IDataChangeNotifier, DataChangeNotifier>();
        builder.Services.AddSingleton(HapticFeedback.Default);
        builder.Services.AddSingleton<IHapticService, HapticService>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<ExerciseListPage>();
        builder.Services.AddSingleton<WorkoutListPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<ExerciseListViewModel>();
        builder.Services.AddTransient<ExerciseFormViewModel>();
        builder.Services.AddTransient<ExerciseFilterViewModel>();
        builder.Services.AddTransient<ExercisePickerViewModel>();
        builder.Services.AddTransient<ExerciseConfigurationViewModel>();
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

        return builder.Build();
    }
}