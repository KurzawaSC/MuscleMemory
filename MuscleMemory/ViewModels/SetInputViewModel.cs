using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;

namespace MuscleMemory.ViewModels;

public sealed partial class SetInputViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string WeightInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string RepsInput { get; set; } = string.Empty;

    public bool IsValid => TryRead(out _);

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
    private void IncreaseWeight() =>
        WeightInput = FormatWeight(ReadWeightOrZero() + DomainDefaults.WeightStepInKg);

    [RelayCommand]
    private void DecreaseWeight() =>
        WeightInput = FormatWeight(Math.Max(0, ReadWeightOrZero() - DomainDefaults.WeightStepInKg));

    [RelayCommand]
    private void IncreaseReps() =>
        RepsInput = FormatReps(ReadRepsOrZero() + DomainDefaults.RepsStep);

    [RelayCommand]
    private void DecreaseReps() =>
        RepsInput = FormatReps(Math.Max(0, ReadRepsOrZero() - DomainDefaults.RepsStep));

    private double ReadWeightOrZero() => TryReadWeight(out double weight) ? weight : 0;

    private int ReadRepsOrZero() => TryReadReps(out int reps) ? reps : 0;

    private bool TryReadWeight(out double weight) =>
        double.TryParse(WeightInput, NumberStyles.Any, CultureInfo.InvariantCulture, out weight);

    private bool TryReadReps(out int reps) =>
        int.TryParse(RepsInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out reps);

    private static string FormatWeight(double weight) => weight.ToString(CultureInfo.InvariantCulture);

    private static string FormatReps(int reps) => reps.ToString(CultureInfo.InvariantCulture);
}
