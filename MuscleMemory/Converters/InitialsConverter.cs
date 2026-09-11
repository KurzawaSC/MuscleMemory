using System.Globalization;

namespace MuscleMemory.Converters;

public sealed class InitialsConverter : IValueConverter
{
    private const int InitialsLength = 2;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var initials = words.Length >= InitialsLength
            ? string.Concat(words.Take(InitialsLength).Select(word => word[0]))
            : words[0][..Math.Min(InitialsLength, words[0].Length)];

        return initials.ToUpper(culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
