namespace MuscleMemory.Data.Repositories;

internal static class SqlPlaceholders
{
    private const string Parameter = "?";
    private const string Separator = ",";

    public static string For(int count) => string.Join(Separator, Enumerable.Repeat(Parameter, count));
}
