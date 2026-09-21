using System.Windows.Input;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ExercisePickerSheet : ContentView
{
    public static readonly BindableProperty PickerProperty =
        BindableProperty.Create(nameof(Picker), typeof(SelectExerciseViewModel), typeof(ExercisePickerSheet));

    public static readonly BindableProperty PickCommandProperty =
        BindableProperty.Create(nameof(PickCommand), typeof(ICommand), typeof(ExercisePickerSheet));

    public static readonly BindableProperty CloseCommandProperty =
        BindableProperty.Create(nameof(CloseCommand), typeof(ICommand), typeof(ExercisePickerSheet));

    public static readonly BindableProperty CreateCommandProperty =
        BindableProperty.Create(nameof(CreateCommand), typeof(ICommand), typeof(ExercisePickerSheet),
            propertyChanged: (bindable, _, _) => ((ExercisePickerSheet)bindable).OnPropertyChanged(nameof(CanCreate)));

    public ExercisePickerSheet() => InitializeComponent();

    public SelectExerciseViewModel? Picker
    {
        get => (SelectExerciseViewModel?)GetValue(PickerProperty);
        set => SetValue(PickerProperty, value);
    }

    public ICommand? PickCommand
    {
        get => (ICommand?)GetValue(PickCommandProperty);
        set => SetValue(PickCommandProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public ICommand? CreateCommand
    {
        get => (ICommand?)GetValue(CreateCommandProperty);
        set => SetValue(CreateCommandProperty, value);
    }

    public bool CanCreate => CreateCommand is not null;
}
