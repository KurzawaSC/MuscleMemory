using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace MuscleMemory;

internal sealed class TabBarShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new TabBarAppearanceTracker(this, shellItem);
}
