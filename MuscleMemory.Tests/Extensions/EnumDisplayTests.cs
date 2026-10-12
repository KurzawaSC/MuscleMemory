using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Extensions;

public sealed class EnumDisplayTests
{
    private const int UndefinedMember = 999;

    [Theory]
    [InlineData(MuscleGroup.Chest, "Chest")]
    [InlineData(MuscleGroup.Back, "Back")]
    [InlineData(MuscleGroup.Shoulders, "Shoulders")]
    [InlineData(MuscleGroup.Biceps, "Biceps")]
    [InlineData(MuscleGroup.Triceps, "Triceps")]
    [InlineData(MuscleGroup.Legs, "Legs")]
    [InlineData(MuscleGroup.Calves, "Calves")]
    [InlineData(MuscleGroup.Abs, "Abs")]
    [InlineData(MuscleGroup.FullBody, "Full Body")]
    [InlineData(MuscleGroup.Other, "Other")]
    public void ToDisplayName_MuscleGroupInEnglish_ReturnsLabel(MuscleGroup muscleGroup, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, muscleGroup.ToDisplayName());
    }

    [Theory]
    [InlineData(EquipmentType.Barbell, "Barbell")]
    [InlineData(EquipmentType.Dumbbell, "Dumbbell")]
    [InlineData(EquipmentType.Machine, "Machine")]
    [InlineData(EquipmentType.Cable, "Cable")]
    [InlineData(EquipmentType.Kettlebell, "Kettlebell")]
    [InlineData(EquipmentType.Band, "Band")]
    [InlineData(EquipmentType.None, "None")]
    [InlineData(EquipmentType.Other, "Other")]
    public void ToDisplayName_EquipmentInEnglish_ReturnsLabel(EquipmentType equipment, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, equipment.ToDisplayName());
    }

    [Theory]
    [InlineData(ThemePreference.System, "System")]
    [InlineData(ThemePreference.Light, "Light")]
    [InlineData(ThemePreference.Dark, "Dark")]
    public void ToDisplayName_ThemeInEnglish_ReturnsLabel(ThemePreference theme, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, theme.ToDisplayName());
    }

    [Fact]
    public void ToDisplayName_InPolish_ReturnsTranslatedLabels()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Całe ciało", MuscleGroup.FullBody.ToDisplayName());
        Assert.Equal("Brak", EquipmentType.None.ToDisplayName());
        Assert.Equal("Systemowy", ThemePreference.System.ToDisplayName());
    }

    [Theory]
    [InlineData(Cultures.EnglishUs)]
    [InlineData(Cultures.Polish)]
    public void ToDisplayName_EveryMember_HasDistinctNonEmptyLabel(string culture)
    {
        using var scope = new CultureScope(culture);

        AssertDistinctLabels(Enum.GetValues<MuscleGroup>().Select(EnumDisplay.ToDisplayName));
        AssertDistinctLabels(Enum.GetValues<EquipmentType>().Select(EnumDisplay.ToDisplayName));
        AssertDistinctLabels(Enum.GetValues<ThemePreference>().Select(EnumDisplay.ToDisplayName));
    }

    [Fact]
    public void ToDisplayName_UndefinedMember_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((MuscleGroup)UndefinedMember).ToDisplayName());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((EquipmentType)UndefinedMember).ToDisplayName());
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ThemePreference)UndefinedMember).ToDisplayName());
    }

    private static void AssertDistinctLabels(IEnumerable<string> labels)
    {
        List<string> list = [.. labels];
        Assert.All(list, label => Assert.False(string.IsNullOrWhiteSpace(label)));
        Assert.Equal(list.Count, list.Distinct().Count());
    }
}
