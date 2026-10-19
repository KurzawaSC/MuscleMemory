using MuscleMemory.Models;
using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public interface IPersonalRecordService
{
    Task<ExerciseRecords> GetRecordsAsync(int exerciseId);
    Task<IReadOnlySet<int>> FindNewRecordsAsync(IReadOnlyCollection<SessionExercise> performedExercises);
}
