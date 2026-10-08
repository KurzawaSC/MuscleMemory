using MuscleMemory.Constants;

namespace MuscleMemory.Markup;

[AcceptEmptyServiceProvider]
public sealed class GutterExtension : IMarkupExtension<Thickness>
{
    public double Start { get; set; } = Spacing.ScreenGutter;

    public double Top { get; set; }

    public double End { get; set; } = Spacing.ScreenGutter;

    public double Bottom { get; set; }

    public Thickness ProvideValue(IServiceProvider serviceProvider) => new(Start, Top, End, Bottom);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
