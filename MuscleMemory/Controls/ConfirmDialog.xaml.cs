namespace MuscleMemory.Controls;

public partial class ConfirmDialog : ContentView
{
    private const double HiddenScale = 0.94;
    private const uint ShowMilliseconds = 180;
    private const uint HideMilliseconds = 150;

    public ConfirmDialog() => InitializeComponent();

    public Task AnimateInAsync() =>
        Task.WhenAll(
            Scrim.FadeToAsync(1, ShowMilliseconds, Easing.CubicOut),
            Card.FadeToAsync(1, ShowMilliseconds, Easing.CubicOut),
            Card.ScaleToAsync(1, ShowMilliseconds, Easing.CubicOut));

    public Task AnimateOutAsync() =>
        Task.WhenAll(
            Scrim.FadeToAsync(0, HideMilliseconds, Easing.CubicIn),
            Card.FadeToAsync(0, HideMilliseconds, Easing.CubicIn),
            Card.ScaleToAsync(HiddenScale, HideMilliseconds, Easing.CubicIn));
}
