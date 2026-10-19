using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public sealed class PersonalRecordService(IWorkoutSetRepository setRepository) : IPersonalRecordService
{
    public async Task<ExerciseRecords> GetRecordsAsync(int exerciseId)
    {
        var repMaxes = await setRepository.GetRepMaxesAsync([exerciseId]);
        var sessionVolumes = await setRepository.GetSessionVolumesAsync(exerciseId);

        return PersonalRecords.Find(repMaxes, sessionVolumes);
    }

    public async Task<IReadOnlySet<int>> FindNewRecordsAsync(IReadOnlyCollection<SessionExercise> performedExercises)
    {
        var history = await setRepository.GetRepMaxesAsync([.. performedExercises.Select(performed => performed.ExerciseId).Distinct()]);
        var loggedSets = await setRepository.GetForSessionExercisesAsync([.. performedExercises.Select(performed => performed.Id)]);
        var performedById = performedExercises.ToDictionary(performed => performed.Id);

        return PersonalRecords.FindNewRecords(history, loggedSets.Select(set => ToPerformedSet(set, performedById[set.SessionExerciseId])));
    }

    private static PerformedSet ToPerformedSet(WorkoutSet set, SessionExercise performed) => new()
    {
        SetId = set.Id,
        SessionId = performed.WorkoutSessionId,
        ExerciseId = performed.ExerciseId,
        Weight = set.Weight,
        Reps = set.Reps
    };
}
