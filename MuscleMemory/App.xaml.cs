using Microsoft.Extensions.DependencyInjection;
using MuscleMemory.Services;

namespace MuscleMemory;

public partial class App : Application
{
    private Window? _window;

    public App(IThemeService themeService, IDatabaseMaintenanceService maintenanceService, IErrorHandler errors)
    {
        InitializeComponent();
        themeService.RestoreSavedTheme();
        errors.ReportFailures(maintenanceService.DeleteExportSnapshotAsync());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_window is { Handler: null })
        {
            return _window;
        }

        var shell = activationState?.Context.Services.GetService<AppShell>();
        return _window = new Window(shell ?? throw new InvalidOperationException("AppShell not found"));
    }
}
