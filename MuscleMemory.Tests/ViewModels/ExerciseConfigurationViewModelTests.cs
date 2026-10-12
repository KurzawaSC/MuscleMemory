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
            new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE),
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

        Assert.Equal(new ExerciseConfiguration(4, 12, 90, 6), _configuration.ToConfiguration());
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
            new ExerciseConfiguration(DomainDefaults.Sets, DomainDefaults.Reps, DomainDefaults.BreakTimeInSeconds, DomainDefaults.TargetRPE),
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

    private static WorkoutExercise Planned(int sets, int reps, int restSeconds, int rpe) => new()
    {
        ExerciseName = BenchPress,
        Sets = sets,
        Reps = reps,
        BreakTimeInSeconds = restSeconds,
        TargetRPE = rpe
    };

    private static List<LevelSegment> FilledUpTo(int rpe) =>
    [
        .. Enumerable.Range(DomainDefaults.MinTargetRPE, DomainDefaults.MaxTargetRPE).Select(value => new LevelSegment(value, value <= rpe))
    ];
}
