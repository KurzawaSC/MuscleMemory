using MuscleMemory.Charting;
using NSubstitute;

namespace MuscleMemory.Tests.Testing;

public static class ChartCanvas
{
    public const float CharacterWidth = 6;
    public const float LabelHeight = 13;

    public static ICanvas Measuring()
    {
        var canvas = Substitute.For<ICanvas>();
        canvas.GetStringSize(Arg.Any<string>(), Arg.Any<IFont>(), Arg.Any<float>())
            .Returns(call => new SizeF(call.Arg<string>().Length * CharacterWidth, LabelHeight));
        return canvas;
    }

    public static ChartModel Model() => new(
        [new ChartPoint(0, 10), new ChartPoint(10, 20), new ChartPoint(20, 30)],
        0,
        20,
        [new AxisTick(0, "Jan"), new AxisTick(20, "March")],
        [new AxisTick(10, "10"), new AxisTick(20, "200"), new AxisTick(30, "30")],
        null,
        null);
}
