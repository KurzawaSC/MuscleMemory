using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class SetInputViewModelTests
{
    private const string ValidReps = "8";
    private const string ValidWeight = "80";

    private readonly SetInputViewModel _input = new();

    [Fact]
    public void New_EmptyInputs_IsInvalidWithoutErrors()
    {
        Assert.False(_input.IsValid);
        Assert.Equal(string.Empty, _input.WeightError);
        Assert.Equal(string.Empty, _input.RepsError);
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, "82.5")]
    [InlineData(Cultures.EnglishUs, "82,5")]
    [InlineData(Cultures.Polish, "82,5")]
    [InlineData(Cultures.Polish, "82.5")]
    public void TryRead_EitherDecimalSeparator_ReadsWeight(string culture, string weight)
    {
        using var scope = new CultureScope(culture);
        _input.WeightInput = weight;
        _input.RepsInput = ValidReps;

        Assert.True(_input.TryRead(out var values));
        Assert.Equal(new SetValues(82.5, 8), values);
        Assert.True(_input.IsValid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.25")]
    [InlineData("22.75")]
    [InlineData("500")]
    public void TryRead_WeightWithinRangeOnQuarterStep_IsValid(string weight)
    {
        _input.WeightInput = weight;
        _input.RepsInput = ValidReps;

        Assert.True(_input.TryRead(out _));
        Assert.Equal(string.Empty, _input.WeightError);
    }

    [Theory]
    [InlineData("500.25")]
    [InlineData("1000")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("1,2,5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void WeightError_UnreadableOrOutOfRange_ShowsRangeError(string weight)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _input.WeightInput = weight;
        _input.RepsInput = ValidReps;

        Assert.False(_input.TryRead(out var values));
        Assert.Null(values);
        Assert.Equal("Enter 0–500 kg", _input.WeightError);
    }

    [Theory]
    [InlineData("82.3")]
    [InlineData("0.1")]
    [InlineData("99.99")]
    public void WeightError_OffQuarterStep_ShowsPrecisionError(string weight)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _input.WeightInput = weight;

        Assert.False(_input.IsValid);
        Assert.Equal("Use 0.25 kg steps", _input.WeightError);
    }

    [Fact]
    public void WeightError_Polish_FormatsNumbersInPolish()
    {
        using var scope = new CultureScope(Cultures.Polish);

        _input.WeightInput = "82,3";

        Assert.Equal("Używaj kroku 0,25 kg", _input.WeightError);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("100")]
    public void TryRead_RepsWithinRange_IsValid(string reps)
    {
        _input.WeightInput = ValidWeight;
        _input.RepsInput = reps;

        Assert.True(_input.TryRead(out _));
        Assert.Equal(string.Empty, _input.RepsError);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    [InlineData("8.5")]
    [InlineData("eight")]
    public void RepsError_UnreadableOrOutOfRange_ShowsRangeError(string reps)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _input.WeightInput = ValidWeight;
        _input.RepsInput = reps;

        Assert.False(_input.TryRead(out _));
        Assert.Equal("Enter 1–100 reps", _input.RepsError);
    }

    [Theory]
    [InlineData(Cultures.EnglishUs, "82.5")]
    [InlineData(Cultures.Polish, "82,5")]
    public void Fill_Values_WritesWeightInCurrentCulture(string culture, string expectedWeight)
    {
        using var scope = new CultureScope(culture);

        _input.Fill(82.5, 8);

        Assert.Equal(expectedWeight, _input.WeightInput);
        Assert.Equal(ValidReps, _input.RepsInput);
        Assert.True(_input.IsValid);
    }

    [Fact]
    public void Clear_FilledInputs_EmptiesBoth()
    {
        _input.Fill(82.5, 8);

        _input.Clear();

        Assert.Equal(string.Empty, _input.WeightInput);
        Assert.Equal(string.Empty, _input.RepsInput);
    }

    [Theory]
    [InlineData("", "2.5")]
    [InlineData("80", "82.5")]
    [InlineData("499", "500")]
    [InlineData("500", "500")]
    [InlineData("abc", "2.5")]
    public void IncreaseWeight_CurrentInput_StepsUpAndClamps(string weight, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _input.WeightInput = weight;

        _input.IncreaseWeightCommand.Execute(null);

        Assert.Equal(expected, _input.WeightInput);
    }

    [Theory]
    [InlineData("", "0")]
    [InlineData("1", "0")]
    [InlineData("82.5", "80")]
    public void DecreaseWeight_CurrentInput_StepsDownAndClamps(string weight, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _input.WeightInput = weight;

        _input.DecreaseWeightCommand.Execute(null);

        Assert.Equal(expected, _input.WeightInput);
    }

    [Fact]
    public void IncreaseWeight_Polish_WritesPolishDecimal()
    {
        using var scope = new CultureScope(Cultures.Polish);
        _input.WeightInput = "80";

        _input.IncreaseWeightCommand.Execute(null);

        Assert.Equal("82,5", _input.WeightInput);
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData("8", "9")]
    [InlineData("100", "100")]
    public void IncreaseReps_CurrentInput_StepsUpAndClamps(string reps, string expected)
    {
        _input.RepsInput = reps;

        _input.IncreaseRepsCommand.Execute(null);

        Assert.Equal(expected, _input.RepsInput);
    }

    [Theory]
    [InlineData("", "1")]
    [InlineData("1", "1")]
    [InlineData("8", "7")]
    public void DecreaseReps_CurrentInput_StepsDownAndClamps(string reps, string expected)
    {
        _input.RepsInput = reps;

        _input.DecreaseRepsCommand.Execute(null);

        Assert.Equal(expected, _input.RepsInput);
    }

    [Fact]
    public void RefreshText_LanguageChanged_RewritesWeightInNewCulture()
    {
        using (new CultureScope(Cultures.EnglishUs))
        {
            _input.Fill(82.5, 8);
        }

        using var scope = new CultureScope(Cultures.Polish);
        _input.RefreshText();

        Assert.Equal("82,5", _input.WeightInput);
    }

    [Fact]
    public void RefreshText_UnreadableWeight_LeavesInputUntouched()
    {
        _input.WeightInput = "abc";

        _input.RefreshText();

        Assert.Equal("abc", _input.WeightInput);
    }

    [Fact]
    public void RefreshText_Always_RaisesErrorTexts()
    {
        List<string?> changed = [];
        _input.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _input.RefreshText();

        Assert.Contains(nameof(SetInputViewModel.WeightError), changed);
        Assert.Contains(nameof(SetInputViewModel.RepsError), changed);
    }

    [Fact]
    public void WeightInput_Changed_RaisesEditedValidityAndError()
    {
        List<string?> changed = [];
        _input.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _input.WeightInput = ValidWeight;

        Assert.Equal(
            [nameof(SetInputViewModel.IsEdited), nameof(SetInputViewModel.WeightInput), nameof(SetInputViewModel.IsValid), nameof(SetInputViewModel.WeightError)],
            changed);
    }

    [Fact]
    public void RepsInput_Changed_RaisesEditedValidityAndError()
    {
        List<string?> changed = [];
        _input.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _input.RepsInput = ValidReps;

        Assert.Equal(
            [nameof(SetInputViewModel.IsEdited), nameof(SetInputViewModel.RepsInput), nameof(SetInputViewModel.IsValid), nameof(SetInputViewModel.RepsError)],
            changed);
    }

    [Fact]
    public void IsEdited_New_IsFalse()
    {
        Assert.False(_input.IsEdited);
    }

    [Fact]
    public void IsEdited_Typed_IsTrue()
    {
        _input.Fill(80, 8);

        _input.RepsInput = "9";

        Assert.True(_input.IsEdited);
    }

    public static TheoryData<Func<SetInputViewModel, IRelayCommand>> Steppers => new(
        input => input.IncreaseWeightCommand,
        input => input.DecreaseWeightCommand,
        input => input.IncreaseRepsCommand,
        input => input.DecreaseRepsCommand);

    [Theory]
    [MemberData(nameof(Steppers))]
    public void IsEdited_Stepped_IsTrue(Func<SetInputViewModel, IRelayCommand> stepper)
    {
        _input.Fill(80, 8);

        stepper(_input).Execute(null);

        Assert.True(_input.IsEdited);
    }

    [Fact]
    public void IsEdited_FilledAfterAnEdit_IsFalseAgain()
    {
        _input.WeightInput = ValidWeight;

        _input.Fill(80, 8);

        Assert.False(_input.IsEdited);
    }

    [Fact]
    public void IsEdited_ClearedAfterAnEdit_IsFalseAgain()
    {
        _input.WeightInput = ValidWeight;

        _input.Clear();

        Assert.False(_input.IsEdited);
    }

    [Fact]
    public void RefreshText_AfterAFill_StaysUnedited()
    {
        _input.Fill(82.5, 8);

        _input.RefreshText();

        Assert.False(_input.IsEdited);
    }

    [Fact]
    public void RefreshText_AfterAnEdit_StaysEdited()
    {
        _input.WeightInput = ValidWeight;

        _input.RefreshText();

        Assert.True(_input.IsEdited);
    }
}
