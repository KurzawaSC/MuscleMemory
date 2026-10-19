using System.Windows.Input;
using MuscleMemory.Controls;
using MuscleMemory.Tests.Testing;
using NSubstitute;

namespace MuscleMemory.Tests.Controls;

public sealed class ProgressChartTests
{
    private const int MiddlePoint = 1;
    private const float TouchY = 90;
    private const float FarAway = ProgressChart.TouchTolerance * 2;

    private static readonly RectF Bounds = new(0, 0, 300, 180);

    private readonly ICommand _select = Substitute.For<ICommand>();
    private readonly ProgressChart _chart;

    public ProgressChartTests()
    {
        _select.CanExecute(Arg.Any<object?>()).Returns(true);
        _chart = new ProgressChart { Model = ChartCanvas.Model(), SelectCommand = _select };
        _chart.Drawable.Draw(ChartCanvas.Measuring(), Bounds);
    }

    private IGraphicsView View => _chart;

    private float PointX(int index) => ((ProgressChartDrawable)_chart.Drawable).PointXs[index];

    [Fact]
    public void StartInteraction_NearAPoint_SelectsIt()
    {
        View.StartInteraction([new PointF(PointX(MiddlePoint) + 5, TouchY)]);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void StartInteraction_AwayFromEveryPoint_ClearsTheSelection()
    {
        _chart.SelectedIndex = MiddlePoint;

        View.StartInteraction([new PointF(PointX(MiddlePoint) + FarAway, TouchY)]);

        _select.Received(1).Execute(null);
    }

    [Fact]
    public void StartInteraction_AwayFromEveryPointWithNothingSelected_DoesNothing()
    {
        View.StartInteraction([new PointF(PointX(MiddlePoint) + FarAway, TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void StartInteraction_OnTheSelectedPoint_DoesNothing()
    {
        _chart.SelectedIndex = MiddlePoint;

        View.StartInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void DragInteraction_OntoAnotherPoint_SelectsIt()
    {
        _chart.SelectedIndex = 0;

        View.DragInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void DragInteraction_AwayFromEveryPoint_KeepsTheSelection()
    {
        _chart.SelectedIndex = MiddlePoint;

        View.DragInteraction([new PointF(PointX(MiddlePoint) + FarAway, TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void StartInteraction_CommandCannotExecute_DoesNotExecuteIt()
    {
        _select.CanExecute(Arg.Any<object?>()).Returns(false);

        View.StartInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void StartInteraction_NoTouches_DoesNothing()
    {
        View.StartInteraction([]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }
}
