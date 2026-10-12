using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class RestPresetTests
{
    private const int PresetSeconds = 90;

    [Fact]
    public void For_Seconds_FormatsLabelAndIsNotCustom()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var preset = RestPreset.For(PresetSeconds);

        Assert.Equal(PresetSeconds, preset.Seconds);
        Assert.Equal("90 s", preset.Label);
        Assert.False(preset.IsCustom);
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, "Other")]
    [InlineData(Cultures.Polish, "Inna")]
    public void Custom_Language_IsCustomWithTranslatedLabel(string culture, string expected)
    {
        using var scope = new CultureScope(culture);

        var preset = RestPreset.Custom();

        Assert.Null(preset.Seconds);
        Assert.True(preset.IsCustom);
        Assert.Equal(expected, preset.Label);
    }
}
