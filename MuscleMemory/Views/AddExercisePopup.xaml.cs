using CommunityToolkit.Maui.Views;

namespace MuscleMemory.Views;

// Zwykły, niegeneryczny Popup
public partial class AddExercisePopup : Popup
{
    // Publiczna właściwość, która przechowa wynik
    public string? ReturnedExerciseName { get; private set; }

    public AddExercisePopup()
    {
        InitializeComponent();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        // Anulowanie: nic nie zapisujemy i zamykamy okno
        ReturnedExerciseName = null;
        await CloseAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        // Znak zapytania naprawia "Dereference of a possibly null reference"
        string? exerciseName = ExerciseNameEntry?.Text;

        if (!string.IsNullOrWhiteSpace(exerciseName))
        {
            // Zapisujemy wpisaną nazwę do naszej zmiennej publicznej
            ReturnedExerciseName = exerciseName.Trim();

            // Zamykamy pop-up wywołując metodę bez argumentów (tak jak pozwala na to API)
            await CloseAsync();
        }
    }
}