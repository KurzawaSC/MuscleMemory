namespace MuscleMemory.Models;

public sealed record StartedSession(int SessionId, List<SessionExercise> Exercises);
