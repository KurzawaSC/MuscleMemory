using Android.Views;
using PlatformView = Android.Views.View;

namespace MuscleMemory;

public static class PlatformTouch
{
    private const float DefaultSlop = 8;

    public static float Slop
    {
        get
        {
            var context = Android.App.Application.Context;
            var configuration = ViewConfiguration.Get(context);
            var density = context.Resources?.DisplayMetrics?.Density;

            return configuration is null || density is not > 0 ? DefaultSlop : configuration.ScaledTouchSlop / density.Value;
        }
    }

    public static void ClaimGesture(Microsoft.Maui.Controls.View view) =>
        (view.Handler?.PlatformView as PlatformView)?.Parent?.RequestDisallowInterceptTouchEvent(true);
}
