using System.Xml.Linq;
using MuscleMemory.Extensions;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Resources.Styles;

public sealed class ColorsTests
{
    private const double MinimumTextContrast = 4.5;
    private const double LuminanceOffset = 0.05;
    private const string LightSuffix = "Light";
    private const string DarkSuffix = "Dark";
    private const string MutedText = "InkMuted";
    private const string KeyAttribute = "Key";
    private const string ResourcesFolder = "Resources";
    private const string StylesFolder = "Styles";
    private const string ColorsFileName = "Colors.xaml";

    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2009/xaml";

    private static readonly Lazy<IReadOnlyDictionary<string, Color>> Roles = new(LoadRoles);

    public static TheoryData<string, string> MutedTextSurfaces => new()
    {
        { "Background", LightSuffix },
        { "Surface", LightSuffix },
        { "Fill", LightSuffix },
        { "InsetFill", LightSuffix },
        { "PrimaryTint", LightSuffix },
        { "Background", DarkSuffix },
        { "Surface", DarkSuffix },
        { "Fill", DarkSuffix },
        { "InsetFill", DarkSuffix },
        { "PrimaryTint", DarkSuffix },
    };

    [Theory]
    [MemberData(nameof(MutedTextSurfaces))]
    public void InkMuted_OnEverySurfaceBehindMutedText_ReachesTextContrast(string surface, string theme)
    {
        var ratio = ContrastRatio(Roles.Value[MutedText + theme], Roles.Value[surface + theme]);

        Assert.True(ratio >= MinimumTextContrast, $"{MutedText}{theme} on {surface}{theme} is {ratio:0.00}:1.");
    }

    private static double ContrastRatio(Color foreground, Color background)
    {
        var lighter = Math.Max(foreground.RelativeLuminance(), background.RelativeLuminance());
        var darker = Math.Min(foreground.RelativeLuminance(), background.RelativeLuminance());
        return (lighter + LuminanceOffset) / (darker + LuminanceOffset);
    }

    private static Dictionary<string, Color> LoadRoles() =>
        XDocument.Load(RepositoryFiles.PathOf(RepositoryFiles.AppProjectFolder, ResourcesFolder, StylesFolder, ColorsFileName))
            .Root!.Elements()
            .ToDictionary(element => (string)element.Attribute(XamlNamespace + KeyAttribute)!, element => Color.FromArgb(element.Value));
}
