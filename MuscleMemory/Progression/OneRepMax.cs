namespace MuscleMemory.Progression;

public static class OneRepMax
{
    public const int EpleyDivisor = 30;
    public const int MaxReps = 12;
    public const int SingleRep = 1;

    public static double? Estimate(double weight, int reps)
    {
        if (weight <= 0 || reps is < SingleRep or > MaxReps)
        {
            return null;
        }

        return reps == SingleRep ? weight : weight * (EpleyDivisor + reps) / EpleyDivisor;
    }
}
