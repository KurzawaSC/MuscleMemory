using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ExerciseHistoryViewModel : ObservableObject, IQueryAttributable
{
    private readonly DatabaseContext _dbContext;
    private int _exerciseId;

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<ExerciseHistoryEntry> History { get; } = [];

    public ExerciseHistoryViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.ExerciseName, out var name))
        {
            ExerciseName = name?.ToString() ?? string.Empty;
        }

        if (query.TryGetValue(QueryKeys.ExerciseId, out var id)
            && int.TryParse(id?.ToString(), out int exerciseId)
            && exerciseId > 0)
        {
            _exerciseId = exerciseId;
            _ = LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var entries = await _dbContext.GetExerciseHistoryAsync(_exerciseId);
        History.Clear();
        foreach (var entry in entries)
        {
            History.Add(entry);
        }
        IsEmpty = !History.Any();
    }
}
