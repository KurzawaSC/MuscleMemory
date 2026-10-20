using MuscleMemory.Constants;
using MuscleMemory.Models;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class ExerciseConfigurationViewModelTests
{
    private const string BenchPress = "Bench Press";
    private const int CustomRestSeconds = 75;

    private readonly ExerciseConfigurationViewModel _configuration = new();

    [Fact]
    public void New_Always_StartsFromTheDefaultPlan()
    {
        Assert.Equal(
            new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE, null, null),
            _configuration.ToConfiguration());
        Assert.Equal(DomainDefaults.BreakTimeInSeconds, _configuration.SelectedRestPreset.Seconds);
        Assert.Equal(FilledUpTo(DomainDefaults.TargetRPE), _configuration.RpeLevels);
    }

    [Fact]
    public void RestPresets_EnglishUs_OfferThePresetsThenOther()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var configuration = new ExerciseConfigurationViewModel();

        Assert.Equal(["60 s", "90 s", "120 s", "Other"], configuration.RestPresets.Select(preset => preset.Label));
    }

    [Fact]
    public void BeginEdit_PresetRest_LoadsThePlanAndSelectsThePreset()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _configuration.BeginEdit(Planned(sets: 4, reps: 12, restSeconds: 90, rpe: 6));

        Assert.Equal(new ExerciseConfiguration(4, 12, 90, 6, null, null), _configuration.ToConfiguration());
        Assert.Equal((BenchPress, true, "Save"), (_configuration.ExerciseName, _configuration.IsEditing, _configuration.ConfirmText));
        Assert.Equal((90, false), (_configuration.SelectedRestPreset.Seconds, _configuration.IsCustomRest));
        Assert.Equal(FilledUpTo(6), _configuration.RpeLevels);
    }

    [Fact]
    public void BeginEdit_RestOutsideThePresets_SelectsOtherAndKeepsTheSeconds()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _configuration.BeginEdit(Planned(sets: 3, reps: 8, restSeconds: CustomRestSeconds, rpe: 7));

        Assert.True(_configuration.IsCustomRest);
        Assert.Equal((CustomRestSeconds, "75 s"), (_configuration.BreakTimeInSeconds, _configuration.BreakTimeText));
    }

    [Fact]
    public void BeginNew_AfterAnEdit_ResetsToTheDefaults()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _configuration.BeginEdit(Planned(sets: 5, reps: 5, restSeconds: CustomRestSeconds, rpe: 9));

        _configuration.BeginNew("Squat");

        Assert.Equal(("Squat", false, "Add to workout"), (_configuration.ExerciseName, _configuration.IsEditing, _configuration.ConfirmText));
        Assert.Equal(
            new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE, null, null),
            _configuration.ToConfiguration());
        Assert.False(_configuration.IsCustomRest);
    }

    [Fact]
    public void SelectedRestPreset_Preset_SetsTheRestAndItsText()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var changed = PropertyChanges.Record(_configuration);

        _configuration.SelectedRestPreset = _configuration.RestPresets.Single(preset => preset.Seconds == 120);

        Assert.Equal((120, "120 s"), (_configuration.BreakTimeInSeconds, _configuration.BreakTimeText));
        Assert.Contains(nameof(ExerciseConfigurationViewModel.BreakTimeText), changed);
        Assert.Contains(nameof(ExerciseConfigurationViewModel.IsCustomRest), changed);
    }

    [Fact]
    public void SelectedRestPreset_Other_KeepsTheCurrentRest()
    {
        _configuration.SelectedRestPreset = _configuration.RestPresets.Single(preset => preset.Seconds == 90);

        _configuration.SelectedRestPreset = _configuration.RestPresets.Single(preset => preset.IsCustom);

        Assert.Equal((90, true), (_configuration.BreakTimeInSeconds, _configuration.IsCustomRest));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void SelectRpe_Level_FillsTheLevelBarUpToIt(int rpe)
    {
        var levels = _configuration.RpeLevels;

        _configuration.SelectRpeCommand.Execute(rpe);

        Assert.Equal(rpe, _configuration.TargetRPE);
        Assert.Same(levels, _configuration.RpeLevels);
        Assert.Equal(FilledUpTo(rpe), _configuration.RpeLevels);
        Assert.Equal(rpe, _configuration.ToConfiguration().TargetRPE);
    }

    [Fact]
    public void New_Always_HasNoRepRangeAndIsValid()
    {
        Assert.Equal((string.Empty, string.Empty), (_configuration.MinRepsInput, _configuration.MaxRepsInput));
        Assert.Equal((string.Empty, string.Empty), (_configuration.MinRepsError, _configuration.MaxRepsError));
        Assert.True(_configuration.IsValid);
    }

    [Fact]
    public void BeginEdit_RepRange_LoadsItAndKeepsItInTheConfiguration()
    {
        _configuration.BeginEdit(Planned(sets: 3, reps: 10, restSeconds: 90, rpe: 8, minReps: 8, maxReps: 12));

        Assert.Equal(("8", "12"), (_configuration.MinRepsInput, _configuration.MaxRepsInput));
        Assert.Equal(new ExerciseConfiguration(3, 10, 90, 8, 8, 12), _configuration.ToConfiguration());
    }

    [Fact]
    public void BeginNew_AfterEditingARange_ClearsTheRange()
    {
        _configuration.BeginEdit(Planned(sets: 3, reps: 10, restSeconds: 90, rpe: 8, minReps: 8, maxReps: 12));

        _configuration.BeginNew("Squat");

        Assert.Equal((string.Empty, string.Empty), (_configuration.MinRepsInput, _configuration.MaxRepsInput));
        Assert.Equal((null, null), (_configuration.ToConfiguration().MinReps, _configuration.ToConfiguration().MaxReps));
    }

    [Theory]
    [InlineData("8", "12", 8, 12)]
    [InlineData("1", "1", 1, 1)]
    [InlineData("10", "10", 10, 10)]
    [InlineData("1", "30", 1, 30)]
    public void RepRange_BothBoundsWithinOneToThirty_IsValidAndConfigured(string min, string max, int expectedMin, int expectedMax)
    {
        _configuration.MinRepsInput = min;
        _configuration.MaxRepsInput = max;

        Assert.True(_configuration.IsValid);
        Assert.Equal((string.Empty, string.Empty), (_configuration.MinRepsError, _configuration.MaxRepsError));
        Assert.Equal((expectedMin, expectedMax), (_configuration.ToConfiguration().MinReps, _configuration.ToConfiguration().MaxReps));
    }

    [Fact]
    public void RepRange_OnlyTheLowerBound_AsksForTheOtherOne()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _configuration.MinRepsInput = "8";

        Assert.False(_configuration.IsValid);
        Assert.Equal((string.Empty, "Enter both or neither"), (_configuration.MinRepsError, _configuration.MaxRepsError));
    }

    [Fact]
    public void RepRange_OnlyTheUpperBound_AsksForTheOtherOne()
    {
        using var scope = new CultureScope(Cultures.Polish);

        _configuration.MaxRepsInput = "12";

        Assert.False(_configuration.IsValid);
        Assert.Equal(("Wpisz obie wartości albo żadną", string.Empty), (_configuration.MinRepsError, _configuration.MaxRepsError));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("31")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("8.5")]
    [InlineData("8,5")]
    [InlineData("abc")]
    public void RepRange_LowerBoundOutsideOneToThirty_ShowsTheAllowedRange(string min)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _configuration.MaxRepsInput = "12";

        _configuration.MinRepsInput = min;

        Assert.False(_configuration.IsValid);
        Assert.Equal(("Enter 1–30 reps", string.Empty), (_configuration.MinRepsError, _configuration.MaxRepsError));
    }

    [Fact]
    public void RepRange_UpperBoundOutsideOneToThirtyInPolish_ShowsTheAllowedRange()
    {
        using var scope = new CultureScope(Cultures.Polish);
        _configuration.MinRepsInput = "8";

        _configuration.MaxRepsInput = "40";

        Assert.False(_configuration.IsValid);
        Assert.Equal((string.Empty, "Wpisz 1–30 powtórzeń"), (_configuration.MinRepsError, _configuration.MaxRepsError));
    }

    [Fact]
    public void RepRange_UpperBoundBelowTheLowerOne_AsksForAtLeastTheLowerOne()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        _configuration.MinRepsInput = "12";
        _configuration.MaxRepsInput = "8";

        Assert.False(_configuration.IsValid);
        Assert.Equal((string.Empty, "Enter at least 12"), (_configuration.MinRepsError, _configuration.MaxRepsError));
    }

    [Fact]
    public void RepRange_ClearedAgain_IsValidWithoutARange()
    {
        _configuration.MinRepsInput = "12";
        _configuration.MaxRepsInput = "8";

        _configuration.MinRepsInput = string.Empty;
        _configuration.MaxRepsInput = string.Empty;

        Assert.True(_configuration.IsValid);
        Assert.Equal((null, null), (_configuration.ToConfiguration().MinReps, _configuration.ToConfiguration().MaxReps));
    }

    [Fact]
    public void MinRepsInput_Typed_RaisesValidityAndBothErrors()
    {
        var changed = PropertyChanges.Record(_configuration);

        _configuration.MinRepsInput = "8";

        Assert.Contains(nameof(ExerciseConfigurationViewModel.IsValid), changed);
        Assert.Contains(nameof(ExerciseConfigurationViewModel.MinRepsError), changed);
        Assert.Contains(nameof(ExerciseConfigurationViewModel.MaxRepsError), changed);
    }

    [Fact]
    public void MaxRepsInput_Typed_RaisesValidityAndBothErrors()
    {
        var changed = PropertyChanges.Record(_configuration);

        _configuration.MaxRepsInput = "12";

        Assert.Contains(nameof(ExerciseConfigurationViewModel.IsValid), changed);
        Assert.Contains(nameof(ExerciseConfigurationViewModel.MinRepsError), changed);
        Assert.Contains(nameof(ExerciseConfigurationViewModel.MaxRepsError), changed);
    }

    [Theory]
    [InlineData("8", "9")]
    [InlineData("29", "30")]
    [InlineData("30", "30")]
    [InlineData("abc", "1")]
    public void IncreaseMinReps_Value_StepsUpWithinOneToThirty(string current, string expected)
    {
        _configuration.MinRepsInput = current;

        _configuration.IncreaseMinRepsCommand.Execute(null);

        Assert.Equal(expected, _configuration.MinRepsInput);
    }

    [Theory]
    [InlineData("12", "11")]
    [InlineData("2", "1")]
    [InlineData("1", "1")]
    [InlineData("45", "30")]
    public void DecreaseMaxReps_Value_StepsDownWithinOneToThirty(string current, string expected)
    {
        _configuration.MaxRepsInput = current;

        _configuration.DecreaseMaxRepsCommand.Execute(null);

        Assert.Equal(expected, _configuration.MaxRepsInput);
    }

    [Fact]
    public void DecreaseMinRepsAndIncreaseMaxReps_Values_StepTheirOwnBound()
    {
        _configuration.MinRepsInput = "8";
        _configuration.MaxRepsInput = "12";

        _configuration.DecreaseMinRepsCommand.Execute(null);
        _configuration.IncreaseMaxRepsCommand.Execute(null);

        Assert.Equal(("7", "13"), (_configuration.MinRepsInput, _configuration.MaxRepsInput));
    }

    private static WorkoutExercise Planned(int sets, int reps, int restSeconds, int rpe, int? minReps = null, int? maxReps = null) => new()
    {
        ExerciseName = BenchPress,
        Sets = sets,
        Reps = reps,
        BreakTimeInSeconds = restSeconds,
        TargetRPE = rpe,
        MinReps = minReps,
        MaxReps = maxReps
    };

    private static List<LevelSegment> FilledUpTo(int rpe) =>
    [
        .. Enumerable.Range(DomainDefaults.MinTargetRPE, DomainDefaults.MaxTargetRPE).Select(value => new LevelSegment(value, value <= rpe))
    ];
}
