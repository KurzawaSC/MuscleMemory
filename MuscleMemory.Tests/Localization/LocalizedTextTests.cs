using System.ComponentModel;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Localization;

public sealed class LocalizedTextTests
{
    private const string MissingKey = "NoSuchResourceKey";

    [Fact]
    public void Indexer_ExistingKey_ReturnsTextInUICulture()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Wszystkie", LocalizedText.Instance[nameof(AppResources.FilterAll)]);
    }

    [Fact]
    public void Indexer_KeyMissingFromLanguage_FallsBackToEnglish()
    {
        using var scope = new CultureScope(Cultures.Czech);

        Assert.Equal("All", LocalizedText.Instance[nameof(AppResources.FilterAll)]);
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsKey()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(MissingKey, LocalizedText.Instance[MissingKey]);
    }

    [Fact]
    public void Refresh_Always_RaisesChangeForAllProperties()
    {
        List<string?> changedProperties = [];
        void OnChanged(object? sender, PropertyChangedEventArgs e) => changedProperties.Add(e.PropertyName);
        LocalizedText.Instance.PropertyChanged += OnChanged;

        try
        {
            LocalizedText.Instance.Refresh();
        }
        finally
        {
            LocalizedText.Instance.PropertyChanged -= OnChanged;
        }

        Assert.Equal([null], changedProperties);
    }
}
