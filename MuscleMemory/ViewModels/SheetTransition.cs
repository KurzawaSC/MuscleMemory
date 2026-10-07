using MuscleMemory.Constants;

namespace MuscleMemory.ViewModels;

public static class SheetTransition
{
    public static async Task CloseAsync(Action close)
    {
        close();
        await Task.Delay(UiTiming.SheetClose);
    }
}
