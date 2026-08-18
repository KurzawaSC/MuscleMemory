using Microsoft.Extensions.DependencyInjection;
using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            string savedTheme = Preferences.Default.Get(PreferenceKeys.AppTheme, nameof(ThemePreference.System));
            UserAppTheme = Enum.Parse<ThemePreference>(savedTheme) switch
            {
                ThemePreference.Light => AppTheme.Light,
                ThemePreference.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = activationState?.Context.Services.GetService<AppShell>();
            return new Window(shell ?? throw new InvalidOperationException("AppShell not found"));
        }
    }
}