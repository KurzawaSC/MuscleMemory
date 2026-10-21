using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Localization;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record SuggestionItem(string Text, string Description)
{
    public static SuggestionItem Create(ProgressionSuggestion suggestion)
    {
        var weight = string.Format(CultureInfo.CurrentCulture, AppResources.WeightFormat, suggestion.Weight);
        var targets = suggestion.TargetReps;
        var isUniform = targets.Distinct().Count() == 1;

        var targetText = isUniform ? FormatReps(targets[0]) : string.Join(DisplayFormats.RepTargetSeparator, targets.Select(FormatReps));
        var weightByReps = string.Format(CultureInfo.CurrentCulture, AppResources.WeightByRepsFormat, weight, targetText);
        var plan = isUniform ? DescribeUniformPlan(targets.Count, targets[0]) : DescribeVaryingPlan(targets);

        return new SuggestionItem(
            string.Format(CultureInfo.CurrentCulture, TextFormat(suggestion.Outcome), weightByReps),
            string.Format(CultureInfo.CurrentCulture, DescriptionFormat(suggestion.Outcome), weight, plan));
    }

    private static string TextFormat(SuggestionOutcome outcome) => outcome switch
    {
        SuggestionOutcome.Increase => AppResources.SuggestionIncreaseFormat,
        SuggestionOutcome.Hold => AppResources.SuggestionHoldFormat,
        SuggestionOutcome.Decrease => AppResources.SuggestionDecreaseFormat,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };

    private static string DescriptionFormat(SuggestionOutcome outcome) => outcome switch
    {
        SuggestionOutcome.Increase => AppResources.SuggestionIncreaseDescriptionFormat,
        SuggestionOutcome.Hold => AppResources.SuggestionHoldDescriptionFormat,
        SuggestionOutcome.Decrease => AppResources.SuggestionDecreaseDescriptionFormat,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };

    private static string DescribeUniformPlan(int sets, int reps) => string.Format(
        CultureInfo.CurrentCulture,
        AppResources.SetsOfRepsFormat,
        Plurals.Format(nameof(AppResources.SetCount_Other), sets),
        Plurals.Format(nameof(AppResources.RepCount_Other), reps));

    private static string DescribeVaryingPlan(IReadOnlyList<int> targets)
    {
        var leading = string.Join(AppResources.ResultSeparator, targets.SkipLast(1).Select(FormatReps));
        var reps = string.Format(CultureInfo.CurrentCulture, AppResources.ListAndFormat, leading, Plurals.Format(nameof(AppResources.RepCount_Other), targets[^1]));
        return string.Format(CultureInfo.CurrentCulture, AppResources.VaryingSetsFormat, reps);
    }

    private static string FormatReps(int reps) => reps.ToString(CultureInfo.CurrentCulture);
}
