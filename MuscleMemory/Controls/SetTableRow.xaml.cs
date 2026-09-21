using MuscleMemory.Models;

namespace MuscleMemory.Controls;

public partial class SetTableRow : ContentView
{
    public static readonly BindableProperty SetProperty =
        BindableProperty.Create(nameof(Set), typeof(WorkoutSet), typeof(SetTableRow));

    public SetTableRow() => InitializeComponent();

    public WorkoutSet? Set
    {
        get => (WorkoutSet?)GetValue(SetProperty);
        set => SetValue(SetProperty, value);
    }
}
