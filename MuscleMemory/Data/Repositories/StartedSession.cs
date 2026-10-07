using MuscleMemory.Models;

namespace MuscleMemory.Data.Repositories;

public sealed record StartedSession(int SessionId, List<SessionExercise> Exercises);
