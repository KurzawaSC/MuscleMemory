using System.Globalization;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.Converters;

public sealed class EnumDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        MuscleGroup muscleGroup => muscleGroup.ToDisplayName(),
        EquipmentType equipment => equipment.ToDisplayName(),
        ThemePreference theme => theme.ToDisplayName(),
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
