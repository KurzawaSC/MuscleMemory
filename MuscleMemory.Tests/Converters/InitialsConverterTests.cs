using System.Globalization;
using MuscleMemory.Converters;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Converters;

public sealed class InitialsConverterTests
{
    private const int NonStringValue = 42;

    private readonly InitialsConverter _converter = new();

    [Theory]
    [InlineData("Push Day", "PD")]
    [InlineData("push day", "PD")]
    [InlineData("Push Day Heavy", "PD")]
    [InlineData("  Upper   Body  ", "UB")]
    public void Convert_SeveralWords_ReturnsFirstLettersOfFirstTwoWords(string name, string expected) =>
        Assert.Equal(expected, Convert(name));

    [Theory]
    [InlineData("Legs", "LE")]
    [InlineData("  legs ", "LE")]
    [InlineData("A", "A")]
    public void Convert_SingleWord_ReturnsUpToFirstTwoLetters(string name, string expected) =>
        Assert.Equal(expected, Convert(name));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Convert_BlankName_ReturnsEmpty(string name) =>
        Assert.Equal(string.Empty, Convert(name));

    [Fact]
    public void Convert_Null_ReturnsEmpty() =>
        Assert.Equal(string.Empty, Convert(null));

    [Fact]
    public void Convert_NonString_ReturnsEmpty() =>
        Assert.Equal(string.Empty, Convert(NonStringValue));

    [Fact]
    public void Convert_TurkishCulture_UppercasesWithCultureRules() =>
        Assert.Equal("İS", _converter.Convert("istanbul", typeof(string), null, CultureInfo.GetCultureInfo(Cultures.Turkish)));

    [Fact]
    public void ConvertBack_Always_Throws() =>
        Assert.Throws<NotSupportedException>(() => _converter.ConvertBack("PD", typeof(string), null, CultureInfo.InvariantCulture));

    private object? Convert(object? value) =>
        _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);
}
