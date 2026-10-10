using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Extensions;

public static class CountCaption
{
    private const int SingularCount = 1;

    public static string Exercises(int count) => Choose(count, AppResources.CaptionExercise, AppResources.CaptionExercises);

    public static string Sets(int count) => Choose(count, AppResources.CaptionSet, AppResources.CaptionSets);

    public static string Reps(int count) => Choose(count, AppResources.CaptionRep, AppResources.CaptionReps);

    public static string Workouts(int count) => Choose(count, AppResources.CaptionWorkout, AppResources.CaptionWorkouts);

    public static string Sessions(int count) => Choose(count, AppResources.CaptionSession, AppResources.CaptionSessions);

    private static string Choose(int count, string singular, string plural) => count == SingularCount ? singular : plural;
}
