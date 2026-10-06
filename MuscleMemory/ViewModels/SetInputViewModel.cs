using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;

namespace MuscleMemory.ViewModels;

public sealed partial class SetInputViewModel : ObservableObject
{
    private const char CommaDecimalSeparator = ',';
    private const char InvariantDecimalSeparator = '.';

    private static readonly string WeightRangeError = string.Format(
        CultureInfo.CurrentCulture, UiText.WeightRangeErrorFormat, DomainDefaults.MinWeightInKg, DomainDefaults.MaxWeightInKg);

    private static readonly string WeightPrecisionError = string.Format(
        CultureInfo.CurrentCulture, UiText.WeightPrecisionErrorFormat, DomainDefaults.WeightPrecisionInKg);

    private static readonly string RepsRangeError = string.Format(
        CultureInfo.CurrentCulture, UiText.RepsRangeErrorFormat, DomainDefaults.MinReps, DomainDefaults.MaxReps);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(WeightError))]
    public partial string WeightInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(RepsError))]
    public partial string RepsInput { get; set; } = string.Empty;

    public bool IsValid => TryRead(out _);

    public string WeightError => IsInvalid(WeightInput, TryReadWeight(out _)) ? DescribeWeightError() : string.Empty;

    public string RepsError => IsInvalid(RepsInput, TryReadReps(out _)) ? RepsRangeError : string.Empty;

    public bool TryRead([NotNullWhen(true)] out SetValues? values)
    {
        if (TryReadWeight(out double weight) && TryReadReps(out int reps))
        {
            values = new SetValues(weight, reps);
            return true;
        }

        values = null;
        return false;
    }

    public void Fill(double weight, int reps)
    {
        WeightInput = FormatWeight(weight);
        RepsInput = FormatReps(reps);
    }

    public void Clear()
    {
        WeightInput = string.Empty;
        RepsInput = string.Empty;
    }

    [RelayCommand]
    private void IncreaseWeight() => StepWeight(DomainDefaults.WeightStepInKg);

    [RelayCommand]
    private void DecreaseWeight() => StepWeight(-DomainDefaults.WeightStepInKg);

    [RelayCommand]
    private void IncreaseReps() => StepReps(DomainDefaults.RepsStep);

    [RelayCommand]
    private void DecreaseReps() => StepReps(-DomainDefaults.RepsStep);

    private void StepWeight(double step) =>
        WeightInput = FormatWeight(Math.Clamp(ParseWeightOrZero() + step, DomainDefaults.MinWeightInKg, DomainDefaults.MaxWeightInKg));

    private void StepReps(int step) =>
        RepsInput = FormatReps(Math.Clamp(ParseRepsOrZero() + step, DomainDefaults.MinReps, DomainDefaults.MaxReps));

    private double ParseWeightOrZero() => TryParseWeight(out double weight) ? weight : 0;

    private int ParseRepsOrZero() => TryParseReps(out int reps) ? reps : 0;

    private bool TryReadWeight(out double weight) =>
        TryParseWeight(out weight) && IsWithinWeightRange(weight) && IsWeightPrecise(weight);

    private string DescribeWeightError() =>
        TryParseWeight(out double weight) && IsWithinWeightRange(weight) ? WeightPrecisionError : WeightRangeError;

    private bool TryReadReps(out int reps) =>
        TryParseReps(out reps) && reps is >= DomainDefaults.MinReps and <= DomainDefaults.MaxReps;

    private bool TryParseWeight(out double weight) =>
        double.TryParse(NormalizeDecimalSeparator(WeightInput), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out weight)
        && double.IsFinite(weight);

    private bool TryParseReps(out int reps) =>
        int.TryParse(RepsInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out reps);

    private static bool IsWithinWeightRange(double weight) =>
        weight is >= DomainDefaults.MinWeightInKg and <= DomainDefaults.MaxWeightInKg;

    private static bool IsWeightPrecise(double weight) =>
        decimal.Remainder((decimal)weight, (decimal)DomainDefaults.WeightPrecisionInKg) == decimal.Zero;

    private static bool IsInvalid(string input, bool isReadable) => input.Length > 0 && !isReadable;

    private static string NormalizeDecimalSeparator(string input) =>
        input.Replace(CommaDecimalSeparator, InvariantDecimalSeparator);

    private static string FormatWeight(double weight) => weight.ToString(CultureInfo.CurrentCulture);

    private static string FormatReps(int reps) => reps.ToString(CultureInfo.InvariantCulture);
}
