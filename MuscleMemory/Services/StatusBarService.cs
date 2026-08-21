using MuscleMemory.Constants;
using MuscleMemory.Extensions;
#if ANDROID
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using View = Android.Views.View;
#endif

namespace MuscleMemory.Services;

public sealed class StatusBarService : IStatusBarService
{
    public void ApplyTheme()
    {
        if (ResolveNavBarBackground() is not { } background)
        {
            return;
        }

        Paint(background, background.PrefersDarkForeground());
    }

    public void TrackNavigation(Shell shell)
    {
        shell.Navigated += (_, _) => ApplyTheme();
    }

    private static Color? ResolveNavBarBackground()
    {
        if (Application.Current is not { } application)
        {
            return null;
        }

        var role = application.RequestedTheme == AppTheme.Dark
            ? ColorRoles.NavBarBackgroundDark
            : ColorRoles.NavBarBackgroundLight;

        return application.Resources.TryGetValue(role, out var color) ? color as Color : null;
    }

#if ANDROID
    private const string ScrimTag = "MuscleMemoryStatusBarScrim";

    private static void Paint(Color background, bool useDarkIcons)
    {
        if (Platform.CurrentActivity is not { Window: { DecorView: { } decorView } window } activity)
        {
            return;
        }

        if (activity.FindViewById<FrameLayout>(Android.Resource.Id.Content) is { } content)
        {
            ResolveScrim(content).SetBackgroundColor(background.ToPlatform());
        }

        if (WindowCompat.GetInsetsController(window, decorView) is { } insetsController)
        {
            insetsController.AppearanceLightStatusBars = useDarkIcons;
        }
    }

    private static View ResolveScrim(FrameLayout content)
    {
        if (content.FindViewWithTag(ScrimTag) is View existingScrim)
        {
            content.BringChildToFront(existingScrim);
            return existingScrim;
        }

        var scrim = new View(content.Context)
        {
            Tag = ScrimTag,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                StatusBarInset(ViewCompat.GetRootWindowInsets(content)),
                GravityFlags.Top)
        };

        ViewCompat.SetOnApplyWindowInsetsListener(scrim, new StatusBarInsetListener());
        content.AddView(scrim);

        return scrim;
    }

    private static int StatusBarInset(WindowInsetsCompat? insets) =>
        insets?.GetInsets(WindowInsetsCompat.Type.StatusBars()) is { } systemBars ? systemBars.Top : 0;

    private sealed class StatusBarInsetListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(View? view, WindowInsetsCompat? insets)
        {
            if (view?.LayoutParameters is { } layoutParameters)
            {
                layoutParameters.Height = StatusBarInset(insets);
                view.RequestLayout();
            }

            return insets ?? WindowInsetsCompat.Consumed!;
        }
    }
#else
    private static void Paint(Color background, bool useDarkIcons)
    {
    }
#endif
}
