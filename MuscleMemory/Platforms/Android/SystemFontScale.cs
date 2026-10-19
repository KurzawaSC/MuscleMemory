namespace MuscleMemory;

public static class SystemFontScale
{
    private const float Unscaled = 1;

    public static float Current => Android.App.Application.Context.Resources?.Configuration?.FontScale ?? Unscaled;
}
