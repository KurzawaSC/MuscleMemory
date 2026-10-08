using Android.Views.Accessibility;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using PlatformView = Android.Views.View;

namespace MuscleMemory;

internal static class CheckedStateMapping
{
    public static void Register() =>
        RadioButtonHandler.Mapper.AppendToMapping(nameof(IRadioButton.IsChecked), (handler, radioButton) =>
        {
            if (handler.PlatformView is CompoundButton)
            {
                return;
            }

            Attach(handler.PlatformView, radioButton);
            handler.PlatformView.Parent?.NotifySubtreeAccessibilityStateChanged(handler.PlatformView, handler.PlatformView, (int)ContentChangeTypes.Undefined);
        });

    private static void Attach(PlatformView platformView, IRadioButton radioButton)
    {
        var current = ViewCompat.GetAccessibilityDelegate(platformView);
        if (!IsAttached(current))
        {
            ViewCompat.SetAccessibilityDelegate(platformView, new CheckedStateAccessibilityDelegate(radioButton, current));
        }
    }

    private static bool IsAttached(AccessibilityDelegateCompat? current) => current switch
    {
        CheckedStateAccessibilityDelegate => true,
        AccessibilityDelegateCompatWrapper wrapper => IsAttached(wrapper.WrappedDelegate),
        _ => false
    };
}
