using MuscleMemory.Models;
using MuscleMemory.Progression;

namespace MuscleMemory.Services;

public interface IProgressionSuggestionService
{
    Task<ProgressionSuggestion?> SuggestAsync(SessionExercise exercise);
}
