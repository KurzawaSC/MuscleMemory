namespace MuscleMemory.Services;

public sealed class HapticService(IHapticFeedback hapticFeedback) : IHapticService
{
    private readonly IHapticFeedback _hapticFeedback = hapticFeedback;

    public void Click()
    {
        if (_hapticFeedback.IsSupported)
        {
            _hapticFeedback.Perform(HapticFeedbackType.Click);
        }
    }
}
