using System.ComponentModel;

namespace MuscleMemory.Tests.Testing;

public static class PropertyChanges
{
    public static List<string?> Record(INotifyPropertyChanged source)
    {
        List<string?> changed = [];
        source.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        return changed;
    }
}
