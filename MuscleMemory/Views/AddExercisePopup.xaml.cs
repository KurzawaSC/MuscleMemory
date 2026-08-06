using CommunityToolkit.Maui.Views;

namespace MuscleMemory.Views;
public partial class AddExercisePopup : Popup
{
    public string? ReturnedExerciseName { get; private set; }

    public AddExercisePopup()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        ReturnedExerciseName = null;
        await CloseAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        string? exerciseName = ExerciseNameEntry?.Text;

        if (!string.IsNullOrWhiteSpace(exerciseName))
        {
            ReturnedExerciseName = exerciseName.Trim();
            await CloseAsync();
        }
    }
}