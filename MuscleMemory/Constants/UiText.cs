namespace MuscleMemory.Constants;

public static class UiText
{
    public const string TitleDeleteExercise = "Delete exercise";
    public const string TitleDeleteWorkout = "Delete workout";
    public const string TitleRemoveExercise = "Remove exercise";
    public const string TitleDeleteSet = "Delete set";
    public const string TitleEditSet = "Edit set";
    public const string TitleAddSet = "Add set";
    public const string TitleError = "Error";
    public const string TitleFinishWorkout = "Finish workout";
    public const string TitleUnsavedChanges = "Unsaved changes";
    public const string TitleHoldOn = "Hold on!";
    public const string TitleSuccess = "Success";
    public const string TitleOops = "Oops!";
    public const string TitleEraseAllData = "Erase all data?";
    public const string TitleExportData = "Export Muscle Memory data";

    public const string ButtonOk = "OK";
    public const string ButtonDelete = "Delete";
    public const string ButtonCancel = "Cancel";
    public const string ButtonDiscard = "Discard";
    public const string ButtonFinish = "Finish";
    public const string ButtonAdd = "Add";
    public const string ButtonSave = "Save";
    public const string ButtonResume = "Resume";
    public const string ButtonAddToWorkout = "Add to workout";
    public const string ButtonAddExercise = "Add exercise";
    public const string ButtonStart = "Start";
    public const string ButtonCreateWorkout = "Create workout";
    public const string ButtonErase = "Erase";
    public const string ButtonAddSetInline = "+ Add set";
    public const string ButtonAddExerciseToSession = "Add exercise to session";
    public const string ButtonAddInline = "+ Add";
    public const string ButtonAddExerciseInline = "+ Add exercise";
    public const string ButtonEdit = "Edit";
    public const string ButtonSaveWorkout = "Save workout";
    public const string ButtonRemoveFromWorkout = "Remove from workout";
    public const string ButtonUndo = "Undo";
    public const string ButtonSaveSet = "Save set";
    public const string ButtonSkipRest = "Skip rest";
    public const string ButtonHistory = "History";
    public const string ButtonDone = "Done";
    public const string ButtonClose = "Close";
    public const string ButtonCreateExercise = "Not on the list? Create a new one";

    public const string BodyDeleteSetConfirmation = "Are you sure you want to delete this set?";
    public const string BodyFinishWorkoutConfirmation = "Are you sure you want to finish and save this workout?";
    public const string BodyFinishUnsavedWorkoutConfirmation = "No sets were logged, so this workout will not be saved. Finish anyway?";
    public const string BodyUnsavedChangesConfirmation = "You have unsaved changes. Are you sure you want to discard them and exit?";
    public const string BodyDataErased = "All your data has been erased.";
    public const string BodyNoDataToExport = "There is no data to export yet.";
    public const string BodyOperationFailed = "Something went wrong. Please try again.";
    public const string BodyExportFailed = "Export failed. Please try again.";
    public const string BodyEraseAllData = "This erases exercises, workouts and all history from this device. It can't be undone.";

    public const string DeleteConfirmationFormat = "Are you sure you want to delete “{0}”?";
    public const string DeleteExerciseFromWorkoutsFormat = DeleteConfirmationFormat + " It will be removed from {1} {2}.";
    public const string DeleteWorkoutConfirmationFormat = DeleteConfirmationFormat + " Session history is kept. The plan itself can't be restored.";
    public const string RemoveExerciseConfirmationFormat = "Are you sure you want to remove “{0}”?";
    public const string WorkoutAlreadyActiveFormat = "'{0}' is still in progress. Finish it before starting another workout.";

    public const string AppName = "Muscle Memory";

    public const string HeaderExercises = "Exercises";
    public const string HeaderWorkouts = "Workouts";
    public const string HeaderSettings = "Settings";
    public const string HeaderNewExercise = "New exercise";
    public const string HeaderEditExercise = "Edit exercise";
    public const string HeaderNewWorkout = "New workout";
    public const string HeaderEditWorkout = "Edit workout";
    public const string HeaderExerciseHistory = "Exercise history";
    public const string HeaderSelectExercise = "Select exercise";

    public const string SubtitleExercises = "Build your movement library";
    public const string SubtitleWorkouts = "Your training plans";

    public const string LabelAppearance = "Appearance";
    public const string LabelData = "Data";
    public const string LabelAppTheme = "App theme";
    public const string LabelWorkoutName = "Workout name";
    public const string LabelSets = "Sets";
    public const string LabelReps = "Reps";
    public const string LabelRest = "Rest";
    public const string LabelCustom = "Custom";
    public const string LabelTargetRpe = "Target RPE";
    public const string LabelRpeEasy = "easy";
    public const string LabelRpeMax = "max";
    public const string LabelWeightInKg = "Weight (kg)";
    public const string LabelThisSession = "This session";
    public const string LabelLoggedSets = "Logged sets";
    public const string LabelVolume = "Volume";
    public const string LabelWorkoutComplete = "Workout complete";
    public const string LabelName = "Name";
    public const string LabelMuscleGroup = "Muscle group";
    public const string LabelEquipment = "Equipment";
    public const string LabelSet = "Set";
    public const string LabelWeight = "Weight";

    public const string PlaceholderWorkoutName = "e.g. Monday Eve";
    public const string PlaceholderExerciseName = "e.g. Incline Bench Press";

    public const string ActionExportData = "Export data";
    public const string ActionEraseAllData = "Erase all data";
    public const string ActionViewHistory = "View history";

    public const string PlaceholderSearchExercises = "Search exercises";
    public const string PlaceholderSearch = "Search";

    public const string EmptyExercisesMessage = "No exercises yet";
    public const string EmptyExercisesDetail = "Add your first exercise, then build a workout from it.";
    public const string EmptyNoMatchingExercises = "No exercises match your search";
    public const string EmptyWorkoutsMessage = "No workouts yet";
    public const string EmptyWorkoutsDetail = "Build a workout from your exercises, set sets and rest — starting takes one tap.";
    public const string EmptyHistoryMessage = "No history yet";
    public const string EmptyExerciseHistory = EmptyHistoryMessage + ". Complete this exercise in a workout to see it here.";
    public const string EmptyWorkoutHistoryDetail = "Finish a session of this workout to see it here.";
    public const string EmptyWorkoutEditor = "This workout is empty.\nAdd exercises and set their sets.";

    public const string HintEditableSession = "This session is editable — you can add missing sets";
    public const string HintSaveWorkout = "Add a name and at least one exercise";
    public const string HintExerciseForm = "Name is required, everything else is optional";
    public const string HintFirstTimeExercise = "First time doing this exercise. Enter weight and reps — next time we will prefill your last session.";
    public const string HintPrefilledFromLastSession = "Weight and reps are prefilled from your last session.";

    public const string ThemeOptionSystem = "System theme";
    public const string ThemeOptionLight = "Light theme";
    public const string ThemeOptionDark = "Dark theme";
    public const string ThemeNameSystem = "System";
    public const string ThemeNameLight = "Light";
    public const string ThemeNameDark = "Dark";

    public const string FilterAll = "All";
    public const string RestPresetOther = "Other";
    public const string RestDurationFormat = "{0} s";
    public const string RestPresetFormat = RestDurationFormat;
    public const string RestChipFormat = RestDurationFormat + " rest";
    public const string SetsByRepsFormat = "{0}" + WeightRepsSeparator + "{1}";
    public const string CountFormat = "{0} {1}";
    public const string CaptionExercise = "exercise";
    public const string CaptionExercises = "exercises";
    public const string CaptionSet = "set";
    public const string CaptionSets = "sets";
    public const string CaptionWorkout = "workout";
    public const string CaptionWorkouts = "workouts";
    public const string CaptionSession = "session";
    public const string CaptionSessions = "sessions";
    public const string CaptionVolume = "volume";
    public const string CaptionTime = "time";
    public const string SessionCountFormat = "{0} {1} · newest first";
    public const string ListSeparator = " · ";
    public const string WeightRepsSeparator = " × ";
    public const string KilogramSuffix = " kg";
    public const string VolumeSuffix = " " + CaptionVolume;
    public const string WorkoutHistoryTitleFormat = "{0}" + ListSeparator + "history";
    public const string ListPairFormat = "{0}" + ListSeparator + "{1}";
    public const string BannerResumeWorkout = "Workout in progress" + ListSeparator + "tap to resume";

    public const string LoadingText = "Loading...";
    public const string FirstTimePerformingExercise = "First time";
    public const string LastSessionPrefix = "Last: ";
    public const string ExerciseProgressFormat = "Exercise {0} of {1}";
    public const string SetProgressWithTotalFormat = "Set {0} of {1}";
    public const string SetProgressFormat = "Set {0}";
    public const string TargetRepsFormat = "Target {0} reps · RPE {1}";
    public const string TargetRpeFormat = "RPE {0}";
    public const string SetResultFormat = "{0}×{1}";
    public const string LoggedSetFormat = "{0} kg × {1}";
    public const string VolumeFormat = "{0:N0} kg";
    public const string WeightFormat = "{0} kg";
    public const string VersionFormat = "Version {0}";
    public const string VolumeNumberFormat = "{0:N0}";
    public const string ShortDateFormat = "MMM dd";
    public const string ShortDateTimeFormat = "MMM dd · HH:mm";
    public const string LongDateFormat = "MMM dd, yyyy";
    public const string SummaryDateFormat = "MMM dd, yyyy · HH:mm";
    public const string SessionDateTimeFormat = "ddd, MMM d · HH:mm";
    public const string SessionStatsFormat = "Time {0} · {1} {2}";
    public const string RestTotalFormat = "of " + RestDurationFormat;
    public const string RestExtensionFormat = "+" + RestDurationFormat;
    public const string ResultSeparator = ", ";
    public const string WeightRangeErrorFormat = "Enter {0}–{1} kg";
    public const string WeightPrecisionErrorFormat = "Use {0} kg steps";
    public const string RepsRangeErrorFormat = "Enter {0}–{1} reps";
    public const string ExerciseNameTakenError = "An exercise with this name already exists";

    public const string ElapsedFormat = @"mm\:ss";
    public const string ElapsedWithHoursFormat = @"h\:mm\:ss";
}
