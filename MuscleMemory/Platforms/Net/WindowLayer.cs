namespace MuscleMemory;

public static class WindowLayer
{
    public static double TopInset => 0;

    public static double BottomInset => 0;

    public static bool TryShow(View layer) => false;

    public static void Hide(View layer)
    {
    }

    public static void DismissKeyboard()
    {
    }

    public static IDisposable ObserveBottomInset(View layer, Action<double> onChanged) => NoRegistration.Instance;

    public static IDisposable InterceptBack(Action onBack) => NoRegistration.Instance;

    private sealed class NoRegistration : IDisposable
    {
        public static NoRegistration Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
