using MuscleMemory.Controls;

namespace MuscleMemory.Tests.Controls;

public sealed class FieldVisualStatesTests
{
    [Theory]
    [InlineData(false, false, FieldVisualStates.Idle)]
    [InlineData(false, true, FieldVisualStates.Invalid)]
    [InlineData(true, false, FieldVisualStates.Editing)]
    [InlineData(true, true, FieldVisualStates.Editing)]
    public void For_FocusAndError_PicksExactlyOneState(bool isFocused, bool hasError, string expected) =>
        Assert.Equal(expected, FieldVisualStates.For(isFocused, hasError));
}
