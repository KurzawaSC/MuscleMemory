using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(WorkoutId), "WorkoutId")]
[QueryProperty(nameof(WorkoutName), "WorkoutName")]
public partial class WorkoutHistoryViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial int WorkoutId { get; set; }

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<WorkoutHistorySession> Sessions { get; set; } = new();

    public WorkoutHistoryViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    async partial void OnWorkoutIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _dbContext.GetWorkoutHistoryAsync(WorkoutId);
        Sessions.Clear();
        foreach (var session in history)
        {
            Sessions.Add(session);
        }
        IsEmpty = !Sessions.Any();
    }
}
