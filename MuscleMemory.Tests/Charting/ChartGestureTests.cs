using MuscleMemory.Charting;

namespace MuscleMemory.Tests.Charting;

public sealed class ChartGestureTests
{
    private const float Slop = 8;
    private const float StartX = 100;
    private const float StartY = 50;

    private readonly ChartGesture _gesture = new(Slop);

    public ChartGestureTests() => _gesture.Begin(StartX, StartY);

    [Fact]
    public void Kind_BeforeAnyTouch_IsNone()
    {
        Assert.Equal(ChartGestureKind.None, new ChartGesture(Slop).Kind);
    }

    [Fact]
    public void Begin_Always_IsPending()
    {
        Assert.Equal(ChartGestureKind.Pending, _gesture.Kind);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(0, -8)]
    [InlineData(5, 6)]
    public void Move_WithinTheSlop_StaysPending(float dx, float dy)
    {
        Assert.Equal(ChartGestureKind.Pending, _gesture.Move(StartX + dx, StartY + dy));
    }

    [Theory]
    [InlineData(9, 0)]
    [InlineData(-30, 20)]
    [InlineData(40, -39)]
    public void Move_BeyondTheSlopMostlySideways_IsAHorizontalDrag(float dx, float dy)
    {
        Assert.Equal(ChartGestureKind.HorizontalDrag, _gesture.Move(StartX + dx, StartY + dy));
    }

    [Theory]
    [InlineData(0, 9)]
    [InlineData(20, -30)]
    [InlineData(-6, 7)]
    public void Move_BeyondTheSlopMostlyUpOrDown_IsAVerticalScroll(float dx, float dy)
    {
        Assert.Equal(ChartGestureKind.VerticalScroll, _gesture.Move(StartX + dx, StartY + dy));
    }

    [Fact]
    public void Move_ExactlyDiagonal_IsAVerticalScroll()
    {
        Assert.Equal(ChartGestureKind.VerticalScroll, _gesture.Move(StartX + 20, StartY + 20));
    }

    [Fact]
    public void Move_HorizontalDragTurningVertical_StaysAHorizontalDrag()
    {
        _gesture.Move(StartX + 30, StartY);

        Assert.Equal(ChartGestureKind.HorizontalDrag, _gesture.Move(StartX + 30, StartY + 200));
    }

    [Fact]
    public void Move_VerticalScrollTurningSideways_StaysAVerticalScroll()
    {
        _gesture.Move(StartX, StartY + 30);

        Assert.Equal(ChartGestureKind.VerticalScroll, _gesture.Move(StartX + 200, StartY + 30));
    }

    [Fact]
    public void EndAsTap_LiftedWithinTheSlop_IsATap()
    {
        _gesture.Move(StartX + 3, StartY + 3);

        Assert.True(_gesture.EndAsTap(StartX + 4, StartY - 4));
    }

    [Fact]
    public void EndAsTap_LiftedBeyondTheSlopWithoutAMove_IsNotATap()
    {
        Assert.False(_gesture.EndAsTap(StartX, StartY + Slop + 1));
    }

    [Fact]
    public void EndAsTap_AfterAHorizontalDragReturningToTheStart_IsNotATap()
    {
        _gesture.Move(StartX + 30, StartY);

        Assert.False(_gesture.EndAsTap(StartX, StartY));
    }

    [Fact]
    public void EndAsTap_Always_EndsTheGesture()
    {
        _gesture.EndAsTap(StartX, StartY);

        Assert.Equal(ChartGestureKind.None, _gesture.Kind);
    }

    [Fact]
    public void EndAsTap_AfterCancel_IsNotATap()
    {
        _gesture.Cancel();

        Assert.False(_gesture.EndAsTap(StartX, StartY));
    }

    [Fact]
    public void Move_AfterCancel_StaysNone()
    {
        _gesture.Cancel();

        Assert.Equal(ChartGestureKind.None, _gesture.Move(StartX + 30, StartY));
    }
}
