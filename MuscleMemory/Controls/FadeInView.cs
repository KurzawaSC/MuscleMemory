namespace MuscleMemory.Controls;

public class FadeInView : ContentView
{
    private const double StartOffset = 8;
    private const uint DurationMilliseconds = 180;

    public FadeInView()
    {
        Opacity = 0;
        TranslationY = StartOffset;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        await Task.WhenAll(
            this.FadeToAsync(1, DurationMilliseconds, Easing.CubicOut),
            this.TranslateToAsync(0, 0, DurationMilliseconds, Easing.CubicOut));
    }
}
