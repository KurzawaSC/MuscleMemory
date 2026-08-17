using CommunityToolkit.Maui.Views;
using MuscleMemory.Models;

namespace MuscleMemory.Views;

public partial class ConfigureExercisePopup : Popup
{
    public class ConfigResult
    {
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int BreakTime { get; set; }
        public int TargetRPE { get; set; }
    }

    public ConfigResult? ReturnedConfig { get; private set; }

    public ConfigureExercisePopup(Exercise selectedExercise)
    {
        InitializeComponent();
        ExerciseNameTitle.Text = selectedExercise.Name;
        RpePicker.SelectedItem = 8;
    }

    public ConfigureExercisePopup(WorkoutExercise exerciseToEdit)
    {
        InitializeComponent();
        ExerciseNameTitle.Text = exerciseToEdit.ExerciseName;
        SetsEntry.Text = exerciseToEdit.Sets.ToString();
        RepsEntry.Text = exerciseToEdit.Reps.ToString();
        BreakEntry.Text = exerciseToEdit.BreakTimeInSeconds.ToString();
        RpePicker.SelectedItem = exerciseToEdit.TargetRPE;
        AddButton.Text = "Save";
    }
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        ReturnedConfig = null;
        await CloseAsync();
    }
    private async void OnAddClicked(object? sender, EventArgs e)
    {
        int sets = int.TryParse(SetsEntry.Text, out int s) ? s : 0;
        int reps = int.TryParse(RepsEntry.Text, out int r) ? r : 0;
        int breakTime = int.TryParse(BreakEntry.Text, out int b) ? b : 0;
        int targetRPE = RpePicker.SelectedItem is int rpe ? rpe : 8;

        ReturnedConfig = new ConfigResult
        {
            Sets = sets,
            Reps = reps,
            BreakTime = breakTime,
            TargetRPE = targetRPE
        };

        await CloseAsync();
    }
}
