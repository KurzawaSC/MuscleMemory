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
    private const float BeyondSlop = 40;
    private const float WithinSlop = 3;

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

    private void Tap(float x, float y = TouchY)
    {
        View.StartInteraction([new PointF(x, y)]);
        View.EndInteraction([new PointF(x, y)], true);
    }

    [Fact]
    public void StartInteraction_NearAPoint_SelectsNothingYet()
    {
        View.StartInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void Tap_NearAPoint_SelectsIt()
    {
        Tap(PointX(MiddlePoint) + 5);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void Tap_MovingWithinTheSlop_SelectsThePoint()
    {
        var x = PointX(MiddlePoint);
        View.StartInteraction([new PointF(x, TouchY)]);
        View.DragInteraction([new PointF(x + WithinSlop, TouchY + WithinSlop)]);
        View.EndInteraction([new PointF(x + WithinSlop, TouchY)], true);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void Tap_AwayFromEveryPoint_ClearsTheSelection()
    {
        _chart.SelectedIndex = MiddlePoint;

        Tap(PointX(MiddlePoint) + FarAway);

        _select.Received(1).Execute(null);
    }

    [Fact]
    public void Tap_AwayFromEveryPointWithNothingSelected_DoesNothing()
    {
        Tap(PointX(MiddlePoint) + FarAway);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void Tap_OnTheSelectedPoint_DoesNothing()
    {
        _chart.SelectedIndex = MiddlePoint;

        Tap(PointX(MiddlePoint));

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void Tap_CommandCannotExecute_DoesNotExecuteIt()
    {
        _select.CanExecute(Arg.Any<object?>()).Returns(false);

        Tap(PointX(MiddlePoint));

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void VerticalScroll_StartingOnAPoint_SelectsNothing()
    {
        var x = PointX(MiddlePoint);
        View.StartInteraction([new PointF(x, TouchY)]);
        View.DragInteraction([new PointF(x, TouchY - BeyondSlop)]);
        View.EndInteraction([new PointF(x, TouchY - BeyondSlop)], true);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void VerticalScroll_TakenOverByThePage_SelectsNothing()
    {
        var x = PointX(MiddlePoint);
        View.StartInteraction([new PointF(x, TouchY)]);
        View.CancelInteraction();
        View.EndInteraction([new PointF(x, TouchY)], true);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void VerticalScroll_DriftingSidewaysOntoAPoint_SelectsNothing()
    {
        View.StartInteraction([new PointF(PointX(0), TouchY)]);
        View.DragInteraction([new PointF(PointX(0), TouchY + BeyondSlop)]);
        View.DragInteraction([new PointF(PointX(MiddlePoint), TouchY + BeyondSlop)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void HorizontalDrag_OntoAnotherPoint_SelectsIt()
    {
        _chart.SelectedIndex = 0;
        View.StartInteraction([new PointF(PointX(0), TouchY)]);

        View.DragInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void HorizontalDrag_ThenMovingVertically_KeepsSelecting()
    {
        var x = PointX(MiddlePoint);
        View.StartInteraction([new PointF(x - BeyondSlop, TouchY)]);
        View.DragInteraction([new PointF(x - (BeyondSlop / 2), TouchY)]);
        _chart.SelectedIndex = 0;
        _select.ClearReceivedCalls();

        View.DragInteraction([new PointF(x, TouchY + (BeyondSlop * 2))]);

        _select.Received(1).Execute(MiddlePoint);
    }

    [Fact]
    public void HorizontalDrag_AwayFromEveryPoint_KeepsTheSelection()
    {
        _chart.SelectedIndex = MiddlePoint;
        View.StartInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        View.DragInteraction([new PointF(PointX(MiddlePoint) + FarAway, TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void HorizontalDrag_Released_DoesNotSelectAgain()
    {
        _chart.SelectedIndex = 0;
        View.StartInteraction([new PointF(PointX(0), TouchY)]);
        View.DragInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        View.EndInteraction([new PointF(PointX(MiddlePoint) + FarAway, TouchY)], true);

        _select.Received(1).Execute(MiddlePoint);
        _select.DidNotReceive().Execute(null);
    }

    [Fact]
    public void DragInteraction_WithoutAStart_DoesNothing()
    {
        View.DragInteraction([new PointF(PointX(MiddlePoint), TouchY)]);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }

    [Fact]
    public void Interactions_NoTouches_DoNothing()
    {
        View.StartInteraction([]);
        View.DragInteraction([]);
        View.EndInteraction([], true);

        _select.DidNotReceiveWithAnyArgs().Execute(default);
    }
}
