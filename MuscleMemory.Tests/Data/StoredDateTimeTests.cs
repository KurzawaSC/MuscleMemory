using MuscleMemory.Data.Repositories;

namespace MuscleMemory.Tests.Data;

public sealed class StoredDateTimeTests
{
    private static readonly DateTime Stored = new(2026, 3, 5, 13, 30, 15, DateTimeKind.Unspecified);

    [Fact]
    public void AsUtc_UnspecifiedKind_MarksUtcWithoutShiftingTime()
    {
        var utc = StoredDateTime.AsUtc(Stored);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(Stored.Ticks, utc.Ticks);
    }

    [Fact]
    public void AsUtc_LocalKind_MarksUtcWithoutShiftingTime()
    {
        var local = DateTime.SpecifyKind(Stored, DateTimeKind.Local);

        var utc = StoredDateTime.AsUtc(local);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(Stored.Ticks, utc.Ticks);
    }

    [Fact]
    public void AsUtc_NullableWithValue_MarksUtc()
    {
        DateTime? stored = Stored;

        var utc = StoredDateTime.AsUtc(stored);

        Assert.Equal(DateTimeKind.Utc, utc?.Kind);
        Assert.Equal(Stored.Ticks, utc?.Ticks);
    }

    [Fact]
    public void AsUtc_NullableWithoutValue_ReturnsNull() =>
        Assert.Null(StoredDateTime.AsUtc((DateTime?)null));
}
