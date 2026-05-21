using CommunityToolkit.Maui.Views;
using MuscleMemory.Models;

namespace MuscleMemory.Views;

public partial class SelectExercisePopup : Popup
{
    // Tu zapiszemy wynik
    public Exercise? SelectedExercise { get; private set; }

    // Konstruktor przyjmuje listę ćwiczeń i ładuje ją do widoku
    public SelectExercisePopup(List<Exercise> availableExercises)
    {
        InitializeComponent();
        ExercisesListView.ItemsSource = availableExercises;
    }

    // Odpala się, gdy użytkownik dotknie któregoś ćwiczenia na liście
    private async void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Exercise chosen)
        {
            SelectedExercise = chosen;

            // 1. Oczyszczamy zaznaczenie, by przerwać wewnętrzne animacje MAUI
            ExercisesListView.SelectedItem = null;

            // 2. Dajemy systemowi 50 milisekund na przetworzenie kliknięcia
            await Task.Delay(50);

            // 3. Teraz bezpiecznie zamykamy okno
            await CloseAsync();
        }
    }
}