namespace MuscleMemory.Controls;

public static class FieldVisualStates
{
    public const string Idle = nameof(Idle);
    public const string Invalid = nameof(Invalid);
    public const string Editing = nameof(Editing);

    public static string For(bool isFocused, bool hasError) => isFocused ? Editing : hasError ? Invalid : Idle;
}
