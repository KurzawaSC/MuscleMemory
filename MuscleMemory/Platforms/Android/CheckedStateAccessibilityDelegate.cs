using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using Microsoft.Maui.Platform;
using PlatformView = Android.Views.View;

namespace MuscleMemory;

internal sealed class CheckedStateAccessibilityDelegate(IRadioButton radioButton, AccessibilityDelegateCompat? wrappedDelegate)
    : AccessibilityDelegateCompatWrapper(wrappedDelegate)
{
    private const string RadioButtonClassName = "android.widget.RadioButton";

    public override void OnInitializeAccessibilityNodeInfo(PlatformView? host, AccessibilityNodeInfoCompat? info)
    {
        base.OnInitializeAccessibilityNodeInfo(host, info);

        if (info is null)
        {
            return;
        }

        info.ClassName = RadioButtonClassName;
        info.Checkable = true;
        info.Checked = radioButton.IsChecked;
    }
}
