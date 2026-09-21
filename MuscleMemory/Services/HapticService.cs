namespace MuscleMemory.Services;

public sealed class HapticService(IHapticFeedback hapticFeedback) : IHapticService
{
    private readonly IHapticFeedback _hapticFeedback = hapticFeedback;

    public void Click() => Perform(HapticFeedbackType.Click);

    public void LongPress() => Perform(HapticFeedbackType.LongPress);

    private void Perform(HapticFeedbackType type)
    {
        if (_hapticFeedback.IsSupported)
        {
            _hapticFeedback.Perform(type);
        }
    }
}
