using CommunityToolkit.Mvvm.ComponentModel;

namespace MuscleMemory.ViewModels;

public sealed partial class ListLoadState : ObservableObject
{
    [ObservableProperty]
    public partial bool IsEmpty { get; private set; }

    [ObservableProperty]
    public partial bool HasItems { get; private set; }

    public void Complete(int itemCount)
    {
        IsEmpty = itemCount == 0;
        HasItems = itemCount > 0;
    }
}
