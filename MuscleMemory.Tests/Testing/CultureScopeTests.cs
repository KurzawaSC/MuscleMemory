using System.Globalization;

namespace MuscleMemory.Tests.Testing;

public sealed class CultureScopeTests
{
    [Fact]
    public void Constructor_SingleName_SetsCultureAndUICulture()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(Cultures.Polish, CultureInfo.CurrentCulture.Name);
        Assert.Equal(Cultures.Polish, CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public void Constructor_SeparateNames_SetsEachCulture()
    {
        using var scope = new CultureScope(Cultures.German, Cultures.Polish);

        Assert.Equal(Cultures.German, CultureInfo.CurrentCulture.Name);
        Assert.Equal(Cultures.Polish, CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public void Dispose_AfterChange_RestoresPreviousCultures()
    {
        using var outer = new CultureScope(Cultures.EnglishUs);

        new CultureScope(Cultures.German, Cultures.Polish).Dispose();

        Assert.Equal(Cultures.EnglishUs, CultureInfo.CurrentCulture.Name);
        Assert.Equal(Cultures.EnglishUs, CultureInfo.CurrentUICulture.Name);
    }
}
