using System.Windows.Input;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ExerciseFormSheet : ContentView
{
    public static readonly BindableProperty FormProperty =
        BindableProperty.Create(nameof(Form), typeof(AddEditExerciseViewModel), typeof(ExerciseFormSheet));

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(ExerciseFormSheet));

    public static readonly BindableProperty ConfirmCommandProperty =
        BindableProperty.Create(nameof(ConfirmCommand), typeof(ICommand), typeof(ExerciseFormSheet));

    public ExerciseFormSheet() => InitializeComponent();

    public AddEditExerciseViewModel? Form
    {
        get => (AddEditExerciseViewModel?)GetValue(FormProperty);
        set => SetValue(FormProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => (ICommand?)GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }
}
