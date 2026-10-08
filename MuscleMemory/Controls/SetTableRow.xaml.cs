using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class SetTableRow : ContentView
{
    public static readonly BindableProperty SetProperty =
        BindableProperty.Create(nameof(Set), typeof(LoggedSetItem), typeof(SetTableRow));

    public SetTableRow() => InitializeComponent();

    public LoggedSetItem? Set
    {
        get => (LoggedSetItem?)GetValue(SetProperty);
        set => SetValue(SetProperty, value);
    }
}
