namespace MuscleMemory.Progression;

public sealed record ProgressionSuggestion(SuggestionOutcome Outcome, double Weight, IReadOnlyList<int> TargetReps);
