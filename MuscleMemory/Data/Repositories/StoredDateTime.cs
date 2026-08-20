namespace MuscleMemory.Data.Repositories;

internal static class StoredDateTime
{
    public static DateTime AsUtc(DateTime stored) => DateTime.SpecifyKind(stored, DateTimeKind.Utc);

    public static DateTime? AsUtc(DateTime? stored) => stored.HasValue ? AsUtc(stored.Value) : null;
}
