using NSubstitute;

namespace MuscleMemory.Tests.Testing;

public static class Route
{
    public static ShellNavigationState Is(string route) =>
        Arg.Is<ShellNavigationState>(state => state.Location.OriginalString == route);
}
