using MuscleMemory.Controls;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Controls;

public sealed class ChartLabelSizesTests
{
    private const float FontSize = 11;
    private const float CharacterWidth = ChartCanvas.CharacterWidth;

    private static readonly IFont Font = new Microsoft.Maui.Graphics.Font("Test");

    [Fact]
    public void Measure_Model_MeasuresTheHeightAndEveryTickLabel()
    {
        var sizes = ChartLabelSizes.Measure(ChartCanvas.Measuring(), ChartCanvas.Model(), Font, FontSize);

        Assert.Equal(ChartCanvas.LabelHeight, sizes.Height);
        Assert.Equal([2 * CharacterWidth, 3 * CharacterWidth, 2 * CharacterWidth], sizes.ValueWidths);
        Assert.Equal([3 * CharacterWidth, 5 * CharacterWidth], sizes.DateWidths);
    }

    [Fact]
    public void ValueWidth_ValueLabelsOfDifferentWidths_IsTheWidest()
    {
        var sizes = ChartLabelSizes.Measure(ChartCanvas.Measuring(), ChartCanvas.Model(), Font, FontSize);

        Assert.Equal(3 * CharacterWidth, sizes.ValueWidth);
    }

    [Fact]
    public void Fits_SameModelAndFontSize_IsTrue()
    {
        var model = ChartCanvas.Model();

        Assert.True(ChartLabelSizes.Measure(ChartCanvas.Measuring(), model, Font, FontSize).Fits(model, FontSize));
    }

    [Fact]
    public void Fits_EqualModelBuiltAgain_IsFalse()
    {
        var model = ChartCanvas.Model();

        Assert.False(ChartLabelSizes.Measure(ChartCanvas.Measuring(), model, Font, FontSize).Fits(model with { }, FontSize));
    }

    [Fact]
    public void Fits_OtherFontSize_IsFalse()
    {
        var model = ChartCanvas.Model();

        Assert.False(ChartLabelSizes.Measure(ChartCanvas.Measuring(), model, Font, FontSize).Fits(model, FontSize * 2));
    }
}
