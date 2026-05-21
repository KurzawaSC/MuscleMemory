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
    }

    public ConfigResult? ReturnedConfig { get; private set; }

    public ConfigureExercisePopup(Exercise selectedExercise)
    {
        InitializeComponent();
        ExerciseNameTitle.Text = selectedExercise.Name;
    }

    // Usunięto znak zapytania przy 'object sender'
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        ReturnedConfig = null;
        await CloseAsync();
    }

    // Usunięto znak zapytania przy 'object sender'
    private async void OnAddClicked(object sender, EventArgs e)
    {
        int sets = int.TryParse(SetsEntry.Text, out int s) ? s : 0;
        int reps = int.TryParse(RepsEntry.Text, out int r) ? r : 0;
        int breakTime = int.TryParse(BreakEntry.Text, out int b) ? b : 0;

        ReturnedConfig = new ConfigResult
        {
            Sets = sets,
            Reps = reps,
            BreakTime = breakTime
        };

        await CloseAsync();
    }
}