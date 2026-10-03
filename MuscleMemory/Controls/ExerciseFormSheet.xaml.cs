using MuscleMemory.ViewModels;

namespace MuscleMemory.Controls;

public partial class ExerciseFormSheet : ContentView
{
    public static readonly BindableProperty FormProperty =
        BindableProperty.Create(nameof(Form), typeof(AddEditExerciseViewModel), typeof(ExerciseFormSheet));

    public ExerciseFormSheet() => InitializeComponent();

    public AddEditExerciseViewModel? Form
    {
        get => (AddEditExerciseViewModel?)GetValue(FormProperty);
        set => SetValue(FormProperty, value);
    }
}
