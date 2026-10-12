using MuscleMemory.Models;
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
}
