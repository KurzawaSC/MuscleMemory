using System.ComponentModel;
using MuscleMemory.Diagnostics;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Localization;

public sealed class LocalizedText : INotifyPropertyChanged
{
    private const string MissingKeyMessage = "Missing resource key: ";

    private static readonly PropertyChangedEventArgs AllProperties = new(null);

    private LocalizedText()
    {
    }

    public static LocalizedText Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => AppResources.ResourceManager.GetString(key) ?? ReportMissing(key);

    public void Refresh() => PropertyChanged?.Invoke(this, AllProperties);

    private static string ReportMissing(string key)
    {
        AppLog.DebugWarning(MissingKeyMessage + key);
        return key;
    }
}
