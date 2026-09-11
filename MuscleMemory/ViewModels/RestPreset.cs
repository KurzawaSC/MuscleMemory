using MuscleMemory.Constants;

namespace MuscleMemory.ViewModels;

public sealed record RestPreset(int? Seconds, string Label)
{
    public static RestPreset Custom { get; } = new(null, UiText.RestPresetOther);

    public static RestPreset For(int seconds) => new(seconds, string.Format(UiText.RestPresetFormat, seconds));

    public bool IsCustom => Seconds is null;
}
