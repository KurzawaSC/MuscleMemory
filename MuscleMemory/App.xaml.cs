using Microsoft.Extensions.DependencyInjection;
using MuscleMemory.Services;

namespace MuscleMemory;

public partial class App : Application
{
    public App(IThemeService themeService)
    {
        InitializeComponent();
        themeService.RestoreSavedTheme();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = activationState?.Context.Services.GetService<AppShell>();
        return new Window(shell ?? throw new InvalidOperationException("AppShell not found"));
    }
}
