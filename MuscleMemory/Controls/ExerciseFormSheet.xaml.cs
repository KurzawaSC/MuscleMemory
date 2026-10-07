using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ExerciseFormSheet : ContentView
{
    public static readonly BindableProperty FormProperty =
        BindableProperty.Create(nameof(Form), typeof(ExerciseFormViewModel), typeof(ExerciseFormSheet));

    public ExerciseFormSheet() => InitializeComponent();

    public ExerciseFormViewModel? Form
    {
        get => (ExerciseFormViewModel?)GetValue(FormProperty);
        set => SetValue(FormProperty, value);
    }
}
