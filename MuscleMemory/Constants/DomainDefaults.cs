namespace MuscleMemory.Constants;

public static class DomainDefaults
{
    public const int Sets = 3;
    public const int Reps = 10;
    public const int BreakTimeInSeconds = 60;
    public const int TargetRPE = 8;
    public const int MinSets = 1;
    public const int MaxSets = 20;
    public const int MinReps = 1;
    public const int MaxReps = 100;
    public const int MinBreakTimeInSeconds = 0;
    public const int MaxBreakTimeInSeconds = 600;
    public const int BreakTimeStepInSeconds = 15;
    public const int RestExtensionInSeconds = 30;
    public const double WeightStepInKg = 2.5;
    public const int RepsStep = 1;
    public static readonly int[] BreakTimePresetsInSeconds = [60, 90, 120];
    public const int MinTargetRPE = 1;
    public const int MaxTargetRPE = 10;
    public const int ActiveWorkoutStateId = 1;
}
