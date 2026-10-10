using System.Globalization;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record RestPreset(int? Seconds, string Label)
{
    public static RestPreset Custom() => new(null, AppResources.RestPresetOther);

    public static RestPreset For(int seconds) => new(seconds, string.Format(CultureInfo.CurrentCulture, AppResources.RestPresetFormat, seconds));

    public bool IsCustom => Seconds is null;
}
