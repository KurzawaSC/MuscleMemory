namespace MuscleMemory.Tests.Testing;

public static class Violations
{
    private const string HeaderFormat = "{0} violation(s):";

    public static void AssertNone(IEnumerable<string> violations)
    {
        List<string> found = [.. violations.Distinct()];
        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found.Prepend(string.Format(HeaderFormat, found.Count))));
    }
}
