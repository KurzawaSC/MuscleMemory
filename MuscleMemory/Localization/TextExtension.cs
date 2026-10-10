namespace MuscleMemory.Localization;

[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TextExtension : IMarkupExtension<BindingBase>
{
    private const string IndexerPathFormat = "[{0}]";

    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(string.Format(IndexerPathFormat, Key), BindingMode.OneWay, source: LocalizedText.Instance);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
