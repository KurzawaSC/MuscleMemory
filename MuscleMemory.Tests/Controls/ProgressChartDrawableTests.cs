using MuscleMemory.Controls;
using MuscleMemory.Tests.Testing;
using NSubstitute;

namespace MuscleMemory.Tests.Controls;

public sealed class ProgressChartDrawableTests
{
    private const int MeasuredStrings = 6;
    private const string WidestValueLabel = "200";

    private static readonly RectF Bounds = new(0, 0, 300, 180);

    [Fact]
    public void Draw_OnlyTheSelectionChanges_MeasuresTheLabelsOnce()
    {
        var canvas = ChartCanvas.Measuring();
        var chart = new ProgressChart { Model = ChartCanvas.Model() };
        var drawable = new ProgressChartDrawable(chart);

        drawable.Draw(canvas, Bounds);
        chart.SelectedIndex = 1;
        drawable.Draw(canvas, Bounds);

        canvas.Received(MeasuredStrings).GetStringSize(Arg.Any<string>(), Arg.Any<IFont>(), Arg.Any<float>());
    }

    [Fact]
    public void Draw_NewModel_MeasuresTheLabelsAgain()
    {
        var canvas = ChartCanvas.Measuring();
        var chart = new ProgressChart { Model = ChartCanvas.Model() };
        var drawable = new ProgressChartDrawable(chart);

        drawable.Draw(canvas, Bounds);
        chart.Model = ChartCanvas.Model();
        drawable.Draw(canvas, Bounds);

        canvas.Received(2 * MeasuredStrings).GetStringSize(Arg.Any<string>(), Arg.Any<IFont>(), Arg.Any<float>());
    }

    [Fact]
    public void Draw_ValueLabels_TheWidestStartsAtTheLeftEdge()
    {
        var canvas = ChartCanvas.Measuring();
        var drawable = new ProgressChartDrawable(new ProgressChart { Model = ChartCanvas.Model() });

        drawable.Draw(canvas, Bounds);

        canvas.Received(1).DrawString(WidestValueLabel, Arg.Is(Bounds.Left), Arg.Any<float>(), HorizontalAlignment.Left);
    }

    [Fact]
    public void Draw_Labels_NeverLeaveTheAlignmentToTheCanvas()
    {
        var canvas = ChartCanvas.Measuring();
        var drawable = new ProgressChartDrawable(new ProgressChart { Model = ChartCanvas.Model() });

        drawable.Draw(canvas, Bounds);

        canvas.DidNotReceive().DrawString(Arg.Any<string>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Is<HorizontalAlignment>(alignment => alignment != HorizontalAlignment.Left));
    }
}
