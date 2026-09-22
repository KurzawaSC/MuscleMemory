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

    public static double BottomInset
    {
        get
        {
            if (Platform.CurrentActivity?.Window?.DecorView is not { } decorView
                || ViewCompat.GetRootWindowInsets(decorView)?.GetInsets(WindowInsetsCompat.Type.NavigationBars()) is not { } insets)
            {
                return 0;
            }

            return decorView.Context.FromPixels(insets.Bottom);
        }
    }

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

    private sealed class BackCallback(Action onBack) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed() => onBack();
    }

    private sealed class BackRegistration(OnBackPressedCallback? callback) : IDisposable
    {
        public void Dispose() => callback?.Remove();
    }
#else
    public static bool TryShow(Microsoft.Maui.Controls.View layer) => false;

    public static void Hide(Microsoft.Maui.Controls.View layer)
    {
    }

    public static double BottomInset => 0;

    public static IDisposable InterceptBack(Action onBack) => new BackRegistration();

    private sealed class BackRegistration : IDisposable
    {
        public void Dispose()
        {
        }
    }
#endif
}
