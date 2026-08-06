using CommunityToolkit.Maui.Views;
using MuscleMemory.Models;

namespace MuscleMemory.Views;

public partial class SelectExercisePopup : Popup
{
    public Exercise? SelectedExercise { get; private set; }
    public SelectExercisePopup(List<Exercise> availableExercises)
    {
        InitializeComponent();
        ExercisesListView.ItemsSource = availableExercises;
    }
    private async void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Exercise chosen)
        {
            SelectedExercise = chosen;
            ExercisesListView.SelectedItem = null;
            await Task.Delay(50);
            await CloseAsync();
        }
    }
}