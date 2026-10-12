using System.Globalization;
using MuscleMemory.Converters;

namespace MuscleMemory.Tests.Converters;

public sealed class AreEqualConverterTests
{
    private readonly AreEqualConverter _converter = new();

    [Fact]
    public void Convert_SameReference_ReturnsTrue()
    {
        var value = new object();

        Assert.Equal(true, Convert(value, value));
    }

    [Fact]
    public void Convert_EqualValues_ReturnsTrue() =>
        Assert.Equal(true, Convert(42, 42));

    [Fact]
    public void Convert_DifferentValues_ReturnsFalse() =>
        Assert.Equal(false, Convert(42, 43));

    [Fact]
    public void Convert_DifferentTypes_ReturnsFalse() =>
        Assert.Equal(false, Convert(42, "42"));

    [Fact]
    public void Convert_BothNull_ReturnsFalse() =>
        Assert.Equal(false, Convert(null, null));

    [Fact]
    public void Convert_FirstNull_ReturnsFalse() =>
        Assert.Equal(false, Convert(null, 42));

    [Fact]
    public void Convert_SecondNull_ReturnsFalse() =>
        Assert.Equal(false, Convert(42, null));

    [Fact]
    public void Convert_SingleValue_ReturnsFalse() =>
        Assert.Equal(false, _converter.Convert([42], typeof(bool), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_ThreeEqualValues_ReturnsFalse() =>
        Assert.Equal(false, _converter.Convert([42, 42, 42], typeof(bool), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_NoValues_ReturnsFalse() =>
        Assert.Equal(false, _converter.Convert([], typeof(bool), null, CultureInfo.InvariantCulture));

    [Fact]
    public void ConvertBack_Always_Throws() =>
        Assert.Throws<NotSupportedException>(() => _converter.ConvertBack(true, [typeof(object), typeof(object)], null, CultureInfo.InvariantCulture));

    private object Convert(object? first, object? second) =>
        _converter.Convert([first!, second!], typeof(bool), null, CultureInfo.InvariantCulture);
}
