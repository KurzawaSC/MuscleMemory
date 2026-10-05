namespace MuscleMemory.Services;

[Flags]
public enum DataArea
{
    Exercises = 1,
    Workouts = 2,
    All = Exercises | Workouts
}
