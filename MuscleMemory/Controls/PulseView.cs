namespace MuscleMemory.Controls;

public class PulseView : ContentView
{
    private const double PulseScale = 1.04;
    private const uint PulsePhaseMilliseconds = 150;

    public static readonly BindableProperty PulseKeyProperty =
        BindableProperty.Create(nameof(PulseKey), typeof(object), typeof(PulseView),
            propertyChanged: (bindable, _, _) => ((PulseView)bindable).OnPulseKeyChanged());

    public static readonly BindableProperty IsPulsingProperty =
        BindableProperty.Create(nameof(IsPulsing), typeof(bool), typeof(PulseView), false);

    public object? PulseKey
    {
        get => GetValue(PulseKeyProperty);
        set => SetValue(PulseKeyProperty, value);
    }

    public bool IsPulsing
    {
        get => (bool)GetValue(IsPulsingProperty);
        set => SetValue(IsPulsingProperty, value);
    }

    private async void OnPulseKeyChanged()
    {
        if (!IsPulsing)
        {
            return;
        }

        await this.ScaleToAsync(PulseScale, PulsePhaseMilliseconds, Easing.CubicOut);
        await this.ScaleToAsync(1, PulsePhaseMilliseconds, Easing.CubicIn);
    }
}
