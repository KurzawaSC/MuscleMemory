using MuscleMemory.Charting;

namespace MuscleMemory.Controls;

internal sealed record ChartLabelSizes(
    ChartModel Model,
    float FontSize,
    float Height,
    IReadOnlyList<float> ValueWidths,
    IReadOnlyList<float> DateWidths)
{
    private const string MeasuredDigit = "0";

    public float ValueWidth => ValueWidths.Max();

    public bool Fits(ChartModel model, float fontSize) => ReferenceEquals(Model, model) && FontSize == fontSize;

    public static ChartLabelSizes Measure(ICanvas canvas, ChartModel model, IFont font, float fontSize) => new(
        model,
        fontSize,
        canvas.GetStringSize(MeasuredDigit, font, fontSize).Height,
        [.. model.YTicks.Select(tick => canvas.GetStringSize(tick.Label, font, fontSize).Width)],
        [.. model.XTicks.Select(tick => canvas.GetStringSize(tick.Label, font, fontSize).Width)]);
}
