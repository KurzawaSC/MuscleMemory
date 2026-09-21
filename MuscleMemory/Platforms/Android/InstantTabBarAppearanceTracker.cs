using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using MuscleMemory.Constants;

namespace MuscleMemory;

internal sealed class InstantTabBarAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
    : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
{
    private const float LabelTextSizeSp = 13;
    private const float LabelLetterSpacingEm = 0.023f;

    private readonly IShellContext _shellContext = shellContext;

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);
        ApplyLabelTypography(bottomView);
    }

    public override void ResetAppearance(BottomNavigationView bottomView)
    {
        base.ResetAppearance(bottomView);
        ApplyLabelTypography(bottomView);
    }

    protected override void SetBackgroundColor(BottomNavigationView bottomView, Color? color)
    {
        if (color is null)
        {
            base.SetBackgroundColor(bottomView, color);
            return;
        }

        bottomView.SetBackgroundColor(color.ToPlatform());
    }

    private void ApplyLabelTypography(BottomNavigationView bottomView)
    {
        if (_shellContext.Shell.Handler?.MauiContext?.Services.GetService<IFontManager>() is not { } fontManager)
        {
            return;
        }

        var typeface = fontManager.GetTypeface(Microsoft.Maui.Font.OfSize(FontFamilies.LilitaOne, LabelTextSizeSp));
        bottomView.SetItemTextAppearanceActiveBoldEnabled(false);

        foreach (var label in FindLabels(bottomView))
        {
            label.Typeface = typeface;
            label.LetterSpacing = LabelLetterSpacingEm;
            label.SetTextSize(ComplexUnitType.Sp, LabelTextSizeSp);
        }
    }

    private static IEnumerable<TextView> FindLabels(ViewGroup parent)
    {
        for (var index = 0; index < parent.ChildCount; index++)
        {
            switch (parent.GetChildAt(index))
            {
                case TextView label:
                    yield return label;
                    break;
                case ViewGroup group:
                    foreach (var nestedLabel in FindLabels(group))
                    {
                        yield return nestedLabel;
                    }
                    break;
            }
        }
    }
}
