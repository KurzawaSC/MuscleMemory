using Microsoft.Extensions.DependencyInjection;
using MuscleMemory.Constants;

namespace MuscleMemory
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            string savedTheme = Preferences.Default.Get(PreferenceKeys.AppTheme, "System");
            UserAppTheme = savedTheme switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
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