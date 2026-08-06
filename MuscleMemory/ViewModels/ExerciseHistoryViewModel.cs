using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(ExerciseId), "ExerciseId")]
[QueryProperty(nameof(ExerciseName), "ExerciseName")]
public partial class ExerciseHistoryViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial int ExerciseId { get; set; }

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<ExerciseHistoryEntry> History { get; set; } = new();

    public ExerciseHistoryViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    async partial void OnExerciseIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var entries = await _dbContext.GetExerciseHistoryAsync(ExerciseId);
        History.Clear();
        foreach (var entry in entries)
        {
            History.Add(entry);
        }
        IsEmpty = !History.Any();
    }
}
