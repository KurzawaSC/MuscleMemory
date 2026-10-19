using System.Globalization;
using MuscleMemory.Localization;
using MuscleMemory.Progression;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.ViewModels;

public sealed record PersonalRecordItem(string ValueText, string Caption, string Description)
{
    public int Row { get; init; }

    public int Column { get; init; }

    public static PersonalRecordItem ForHeaviestWeight(PerformedSet set)
    {
        var date = FormatDate(set.DateUtc);

        return new PersonalRecordItem(
            Format(AppResources.LoggedSetFormat, set.Weight, set.Reps),
            Format(AppResources.RecordCaptionFormat, AppResources.RecordHeaviestWeight, date),
            Format(AppResources.HeaviestWeightDescriptionFormat, Format(AppResources.WeightFormat, set.Weight), FormatReps(set.Reps), date));
    }

    public static PersonalRecordItem ForBestOneRepMax(OneRepMaxRecord record)
    {
        var value = Format(AppResources.OneRepMaxFormat, record.Value);
        var date = FormatDate(record.Set.DateUtc);

        return new PersonalRecordItem(
            value,
            Format(AppResources.RecordCaptionFormat, AppResources.RecordBestOneRepMax, date),
            Format(AppResources.BestOneRepMaxDescriptionFormat, value, date));
    }

    public static PersonalRecordItem ForBestSessionVolume(SessionVolume session)
    {
        var value = Format(AppResources.VolumeFormat, session.Volume);
        var date = FormatDate(session.DateUtc);

        return new PersonalRecordItem(
            value,
            Format(AppResources.RecordCaptionFormat, AppResources.RecordBestSessionVolume, date),
            Format(AppResources.BestSessionVolumeDescriptionFormat, value, date));
    }

    public static PersonalRecordItem ForMostReps(PerformedSet set)
    {
        var date = FormatDate(set.DateUtc);

        return new PersonalRecordItem(
            set.Reps.ToString(CultureInfo.CurrentCulture),
            Format(AppResources.RecordCaptionFormat, AppResources.RecordMostReps, date),
            Format(AppResources.MostRepsDescriptionFormat, FormatReps(set.Reps), date));
    }

    private static string FormatDate(DateTime dateUtc) =>
        dateUtc.ToLocalTime().ToString(AppResources.LongDateFormat, CultureInfo.CurrentCulture);

    private static string FormatReps(int reps) => Plurals.Format(nameof(AppResources.RepCount_Other), reps);

    private static string Format(string format, params object[] values) =>
        string.Format(CultureInfo.CurrentCulture, format, values);
}
