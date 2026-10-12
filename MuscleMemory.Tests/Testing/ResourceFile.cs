using System.Xml.Linq;
using MuscleMemory.Localization;

namespace MuscleMemory.Tests.Testing;

public sealed record ResourceFile(string Language, string Path, IReadOnlyDictionary<string, string> Values)
{
    private const string DataElement = "data";
    private const string NameAttribute = "name";
    private const string TypeAttribute = "type";
    private const string ValueElement = "value";
    private const char CategorySeparator = '_';

    public string FileName => System.IO.Path.GetFileName(Path);

    public static ResourceFile Load(string language, string path) => new(
        language,
        path,
        XDocument.Load(path).Root!.Elements(DataElement)
            .Where(data => data.Attribute(TypeAttribute) is null)
            .ToDictionary(data => data.Attribute(NameAttribute)!.Value, data => data.Element(ValueElement)?.Value ?? string.Empty));

    public static string? PluralFamily(string key) =>
        key.LastIndexOf(CategorySeparator) is var separator and > 0 && Enum.TryParse<PluralCategory>(key[(separator + 1)..], out _)
            ? key[..separator]
            : null;

    public static PluralCategory PluralCategoryOf(string key) => Enum.Parse<PluralCategory>(key[(key.LastIndexOf(CategorySeparator) + 1)..]);

    public static string PluralKey(string family, PluralCategory category) => $"{family}{CategorySeparator}{category}";
}
