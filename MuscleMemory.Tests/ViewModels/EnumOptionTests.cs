using MuscleMemory.Models;
using MuscleMemory.Progression;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class EnumOptionTests
{
    [Fact]
    public void For_MuscleGroup_CarriesValueAndLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var option = EnumOption.For(MuscleGroup.FullBody);

        Assert.Equal(MuscleGroup.FullBody, option.Value);
        Assert.Equal("Full Body", option.Label);
    }

    [Fact]
    public void For_Equipment_CarriesValueAndTranslatedLabel()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var option = EnumOption.For(EquipmentType.None);

        Assert.Equal(EquipmentType.None, option.Value);
        Assert.Equal("Brak", option.Label);
    }

    [Fact]
    public void For_MuscleGroup_IsDescribedByItsLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("Chest", EnumOption.For(MuscleGroup.Chest).Description);
    }

    [Fact]
    public void For_ProgressMetric_HasAShortLabelAndASpokenDescription()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var option = EnumOption.For(ProgressMetric.OneRepMax);

        Assert.Equal((ProgressMetric.OneRepMax, "e1RM", "Estimated one-rep max"), ((ProgressMetric)option.Value, option.Label, option.Description));
    }

    [Fact]
    public void For_ProgressRange_HasAShortLabelAndASpokenDescription()
    {
        using var scope = new CultureScope(Cultures.Polish);

        var option = EnumOption.For(ProgressRange.SixMonths);

        Assert.Equal((ProgressRange.SixMonths, "6 mies.", "ostatnie 6 miesięcy"), ((ProgressRange)option.Value, option.Label, option.Description));
    }
}
