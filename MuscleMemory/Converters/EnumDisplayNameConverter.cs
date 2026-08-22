using System.Globalization;
using MuscleMemory.Extensions;

namespace MuscleMemory.Converters;

public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Enum enumValue ? enumValue.ToDisplayName() : string.Empty;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
