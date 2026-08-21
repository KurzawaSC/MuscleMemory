using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace MuscleMemory;

internal sealed class InstantTabBarAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
    : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
{
    protected override void SetBackgroundColor(BottomNavigationView bottomView, Color? color)
    {
        if (color is null)
        {
            base.SetBackgroundColor(bottomView, color);
            return;
        }

        bottomView.SetBackgroundColor(color.ToPlatform());
    }
}
