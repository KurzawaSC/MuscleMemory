using MuscleMemory.Constants;

namespace MuscleMemory.Extensions;

public static class CountCaption
{
    private const int SingularCount = 1;

    public static string Exercises(int count) => Choose(count, UiText.CaptionExercise, UiText.CaptionExercises);

    public static string Sets(int count) => Choose(count, UiText.CaptionSet, UiText.CaptionSets);

    public static string Workouts(int count) => Choose(count, UiText.CaptionWorkout, UiText.CaptionWorkouts);

    public static string Sessions(int count) => Choose(count, UiText.CaptionSession, UiText.CaptionSessions);

    private static string Choose(int count, string singular, string plural) => count == SingularCount ? singular : plural;
}
