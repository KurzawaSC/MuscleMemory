using MuscleMemory.Constants;

namespace MuscleMemory.ViewModels;

public static class SheetTransition
{
    public static async Task CloseAsync(Action close, TimeProvider clock)
    {
        close();
        await Task.Delay(UiTiming.SheetClose, clock);
    }
}
