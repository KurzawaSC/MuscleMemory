using MuscleMemory.Data.Repositories;

namespace MuscleMemory.Tests.Data;

public sealed class SqlPlaceholdersTests
{
    [Theory]
    [InlineData(1, "?")]
    [InlineData(2, "?,?")]
    [InlineData(5, "?,?,?,?,?")]
    public void For_PositiveCount_JoinsOneParameterPerValue(int count, string expected) =>
        Assert.Equal(expected, SqlPlaceholders.For(count));

    [Fact]
    public void For_Zero_ReturnsEmpty() =>
        Assert.Equal(string.Empty, SqlPlaceholders.For(0));
}
