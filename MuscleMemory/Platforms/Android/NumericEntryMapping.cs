using Microsoft.Maui.Handlers;

namespace MuscleMemory;

internal static class NumericEntryMapping
{
    private static readonly string[] InputTypeMappings =
    [
        nameof(IEntry.Keyboard),
        nameof(IEntry.IsReadOnly),
        nameof(IEntry.IsPassword),
        nameof(IEntry.IsTextPredictionEnabled),
        nameof(IEntry.IsSpellCheckEnabled)
    ];

    public static void Register()
    {
        foreach (var mapping in InputTypeMappings)
        {
            EntryHandler.Mapper.AppendToMapping(mapping, ApplyDecimalKeyListener);
        }
    }

    private static void ApplyDecimalKeyListener(IEntryHandler handler, IEntry entry)
    {
        if (entry.Keyboard == Keyboard.Numeric)
        {
            handler.PlatformView.KeyListener = DecimalKeyListener.Instance;
        }
    }
}
