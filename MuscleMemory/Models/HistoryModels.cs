using System;
using System.Collections.Generic;

namespace MuscleMemory.Models;

public class ExerciseHistoryEntry
{
    public DateTime Date { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public List<WorkoutSet> Sets { get; set; } = new();
}

public class WorkoutHistorySession
{
    public DateTime StartTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;
    public DateTime? EndTime { get; set; }
    public double TotalVolume { get; set; }
    public List<WorkoutHistoryExercise> Exercises { get; set; } = new();
}

public class WorkoutHistoryExercise
{
    public string ExerciseName { get; set; } = string.Empty;
    public List<WorkoutSet> Sets { get; set; } = new();
}
