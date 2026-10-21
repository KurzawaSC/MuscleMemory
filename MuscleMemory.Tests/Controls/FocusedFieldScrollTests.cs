using MuscleMemory.Controls;

namespace MuscleMemory.Tests.Controls;

public sealed class FocusedFieldScrollTests
{
    private const double ContentHeight = 638;
    private const double CoveredViewport = 378;
    private const double FieldHeight = 56;
    private const double MaximumScrollY = ContentHeight - CoveredViewport;
    private const double SubPixel = 0.3;

    [Fact]
    public void TargetScrollY_ContentFitsTheViewport_IsNull()
    {
        var scroll = Scroll(viewportHeight: ContentHeight + 1, fieldTop: 500);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_ContentExactlyFillsTheViewport_IsNull()
    {
        var scroll = Scroll(viewportHeight: ContentHeight, fieldTop: 500);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_Detached_IsNull()
    {
        var scroll = Scroll(fieldTop: 500, isAttached: false);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_ViewportNotLaidOut_IsNull()
    {
        var scroll = Scroll(viewportHeight: 0, fieldTop: 500);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldFullyVisible_IsNull()
    {
        var scroll = Scroll(fieldTop: 100);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldTouchingTheViewportBottom_IsNull()
    {
        var scroll = Scroll(fieldTop: CoveredViewport - FieldHeight);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldCoveredBySubPixel_IsNull()
    {
        var scroll = Scroll(fieldTop: CoveredViewport - FieldHeight + SubPixel);

        Assert.Null(scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldCoveredBelowTheViewport_AlignsTheFieldBottomWithTheViewportBottom()
    {
        const double fieldTop = 400;
        var scroll = Scroll(fieldTop: fieldTop);

        Assert.Equal(fieldTop + FieldHeight - CoveredViewport, scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldPartlyCoveredBelowTheViewport_AlignsTheFieldBottomWithTheViewportBottom()
    {
        const double fieldTop = 350;
        var scroll = Scroll(fieldTop: fieldTop);

        Assert.Equal(fieldTop + FieldHeight - CoveredViewport, scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_FieldAboveTheViewport_AlignsTheFieldTopWithTheViewportTop()
    {
        const double fieldTop = 40;
        var scroll = Scroll(scrollY: 200, fieldTop: fieldTop);

        Assert.Equal(fieldTop, scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_TargetBeyondTheContentEnd_IsClampedToTheMaximum()
    {
        var scroll = Scroll(fieldTop: ContentHeight - FieldHeight + 10);

        Assert.Equal(MaximumScrollY, scroll.TargetScrollY());
    }

    [Fact]
    public void TargetScrollY_ClampedTargetIsTheCurrentPosition_IsNull()
    {
        var scroll = Scroll(scrollY: MaximumScrollY, fieldTop: ContentHeight - FieldHeight + 10);

        Assert.Null(scroll.TargetScrollY());
    }

    private static FocusedFieldScroll Scroll(
        double fieldTop,
        double viewportHeight = CoveredViewport,
        double scrollY = 0,
        bool isAttached = true) =>
        new(isAttached, ContentHeight, viewportHeight, scrollY, fieldTop, FieldHeight);
}
