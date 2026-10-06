using Android.App;
using Android.Content.PM;
using Android.OS;
using MuscleMemory.Services;

namespace MuscleMemory;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private InFlightNavigationBackCallback? _inFlightNavigationBack;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var services = IPlatformApplication.Current?.Services;
        services?.GetService<IStatusBarService>()?.ApplyTheme();

        if (services?.GetService<INavigationService>() is { } navigation)
        {
            _inFlightNavigationBack = new InFlightNavigationBackCallback(navigation);
            OnBackPressedDispatcher.AddCallback(this, _inFlightNavigationBack);
        }
    }

    protected override void OnDestroy()
    {
        _inFlightNavigationBack?.Release();
        _inFlightNavigationBack = null;
        base.OnDestroy();
    }
}
