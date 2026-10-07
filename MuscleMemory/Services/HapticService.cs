namespace MuscleMemory.Services;

public sealed class HapticService(IHapticFeedback hapticFeedback) : IHapticService
{
    public void Click() => Perform(HapticFeedbackType.Click);

    public void LongPress() => Perform(HapticFeedbackType.LongPress);

    private void Perform(HapticFeedbackType type)
    {
        if (hapticFeedback.IsSupported)
        {
            hapticFeedback.Perform(type);
        }
    }
}
