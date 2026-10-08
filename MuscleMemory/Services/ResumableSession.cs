using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed record ResumableSession(ActiveWorkoutState State, WorkoutSession Session);
