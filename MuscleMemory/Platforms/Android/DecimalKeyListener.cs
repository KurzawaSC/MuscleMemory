using Android.Text;
using Android.Text.Method;
using Java.Lang;

namespace MuscleMemory;

internal sealed class DecimalKeyListener : NumberKeyListener
{
    private const string AcceptedCharacters = "0123456789.,";

    private static readonly char[] AcceptedCharacterArray = AcceptedCharacters.ToCharArray();

    public static DecimalKeyListener Instance { get; } = new();

    public override InputTypes InputType => InputTypes.ClassNumber | InputTypes.NumberFlagDecimal;

    protected override char[] GetAcceptedChars() => AcceptedCharacterArray;

    public override ICharSequence? FilterFormatted(ICharSequence? source, int start, int end, ISpanned? dest, int dstart, int dend) =>
        IsAccepted(source, start, end) ? null : new Java.Lang.String(string.Empty);

    private static bool IsAccepted(ICharSequence? source, int start, int end) =>
        source is null || source.ToString()[start..end].All(AcceptedCharacters.Contains);
}
