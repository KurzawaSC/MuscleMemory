using System.Globalization;

namespace MuscleMemory.Converters;

public sealed class AreEqualConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [var first, var second] && first is not null && Equals(first, second);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
