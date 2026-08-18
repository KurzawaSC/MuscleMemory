using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MuscleMemory.Models;

public class ExerciseHistoryEntry
{
    public DateTime Date { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public List<WorkoutSet> Sets { get; set; } = new();
}

public class WorkoutHistorySession
{
    public int SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public DateTime? EndTime { get; set; }
    public double TotalVolume { get; set; }
    public ObservableCollection<WorkoutHistoryExercise> Exercises { get; set; } = new();
}

public partial class WorkoutHistoryExercise : ObservableObject
{
    [ObservableProperty]
    public partial int WorkoutExerciseId { get; set; }
    [ObservableProperty]
    public partial int WorkoutSessionId { get; set; }
    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;
    
    public ObservableCollection<WorkoutSet> Sets { get; set; } = new();
}
