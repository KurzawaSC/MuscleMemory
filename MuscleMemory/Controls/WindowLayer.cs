#if ANDROID
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Activity;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using PlatformView = Android.Views.View;
#endif

namespace MuscleMemory.Controls;

public static class WindowLayer
{
#if ANDROID
    public static bool TryShow(Microsoft.Maui.Controls.View layer)
    {
        if (Platform.CurrentActivity?.Window?.DecorView is not ViewGroup decorView
            || Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext is not { } mauiContext)
        {
            return false;
        }

        var platformView = layer.ToPlatform(mauiContext);
        (platformView.Parent as ViewGroup)?.RemoveView(platformView);
        decorView.AddView(platformView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        return true;
    }

    public static void Hide(Microsoft.Maui.Controls.View layer)
    {
        if (layer.Handler?.PlatformView is PlatformView platformView)
        {
            DismissKeyboard(platformView);
            (platformView.Parent as ViewGroup)?.RemoveView(platformView);
        }
    }

    private static void DismissKeyboard(PlatformView platformView)
    {
        if (platformView.FindFocus() is not { } focusedView)
        {
            return;
        }

        var inputMethodManager = platformView.Context?.GetSystemService(Context.InputMethodService) as InputMethodManager;
        inputMethodManager?.HideSoftInputFromWindow(focusedView.WindowToken, HideSoftInputFlags.None);
        focusedView.ClearFocus();
    }

    public static double TopInset => ReadRootInsets(insets => insets.GetInsets(WindowInsetsCompat.Type.StatusBars())?.Top ?? 0);

    public static double BottomInset => ReadRootInsets(BottomPixels);

    public static IDisposable ObserveBottomInset(Microsoft.Maui.Controls.View layer, Action<double> onChanged)
    {
        if (layer.Handler?.PlatformView is not PlatformView platformView)
        {
            return new InsetRegistration(null);
        }

        ViewCompat.SetOnApplyWindowInsetsListener(platformView, new BottomInsetListener(onChanged));
        ViewCompat.RequestApplyInsets(platformView);
        return new InsetRegistration(platformView);
    }

    private static double ReadRootInsets(Func<WindowInsetsCompat, int> selectPixels)
    {
        if (Platform.CurrentActivity?.Window?.DecorView is not { } decorView
            || ViewCompat.GetRootWindowInsets(decorView) is not { } insets)
        {
            return 0;
        }

        return decorView.Context.FromPixels(selectPixels(insets));
    }

    private static int BottomPixels(WindowInsetsCompat insets) =>
        Math.Max(insets.GetInsets(WindowInsetsCompat.Type.Ime())?.Bottom ?? 0,
                 insets.GetInsets(WindowInsetsCompat.Type.NavigationBars())?.Bottom ?? 0);

    public static IDisposable InterceptBack(Action onBack)
    {
        if (Platform.CurrentActivity is not ComponentActivity activity)
        {
            return new BackRegistration(null);
        }

        var callback = new BackCallback(onBack);
        activity.OnBackPressedDispatcher.AddCallback(callback);
        return new BackRegistration(callback);
    }

    private sealed class BottomInsetListener(Action<double> onChanged) : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(PlatformView? view, WindowInsetsCompat? insets)
        {
            if (view is not null && insets is not null)
            {
                onChanged(view.Context.FromPixels(BottomPixels(insets)));
            }

            return insets;
        }
    }

    private sealed class InsetRegistration(PlatformView? platformView) : IDisposable
    {
        public void Dispose()
        {
            if (platformView is not null)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(platformView, null);
            }
        }
    }

    private sealed class BackCallback(Action onBack) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => onBack();
    }

    private sealed class BackRegistration(OnBackPressedCallback? callback) : IDisposable
    {
        public void Dispose() => callback?.Remove();
    }
#endif
}
