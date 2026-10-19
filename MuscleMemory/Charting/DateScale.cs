using System.Diagnostics;

namespace MuscleMemory.Charting;

public static class DateScale
{
    public const int MaxTicks = 4;

    private const int MonthsPerYear = 12;
    private const int YearMagnitudeBase = 10;
    private static readonly int[] DaySteps = [1, 2, 7, 14];
    private static readonly int[] MonthSteps = [1, 2, 3, 6];
    private static readonly int[] YearMantissas = [1, 2, 5];

    public static DateTicks For(DateOnly first, DateOnly last)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first, last);

        foreach (var candidate in Candidates(first, last))
        {
            if (candidate.Dates.Count <= MaxTicks)
            {
                return candidate;
            }
        }

        throw new UnreachableException();
    }

    private static IEnumerable<DateTicks> Candidates(DateOnly first, DateOnly last)
    {
        foreach (var step in DaySteps)
        {
            yield return new DateTicks([.. Days(first, last).Where(date => date.DayNumber % step == 0)], DateTickUnit.Day);
        }

        foreach (var step in MonthSteps)
        {
            yield return new DateTicks([.. MonthStarts(first, last).Where(date => MonthIndex(date) % step == 0)], DateTickUnit.Month);
        }

        for (var magnitude = 1; ; magnitude *= YearMagnitudeBase)
        {
            foreach (var mantissa in YearMantissas)
            {
                var step = mantissa * magnitude;
                yield return new DateTicks([.. YearStarts(first, last).Where(date => date.Year % step == 0)], DateTickUnit.Year);
            }
        }
    }

    private static IEnumerable<DateOnly> Days(DateOnly first, DateOnly last)
    {
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private static IEnumerable<DateOnly> MonthStarts(DateOnly first, DateOnly last)
    {
        for (var date = new DateOnly(first.Year, first.Month, 1); date <= last; date = date.AddMonths(1))
        {
            if (date >= first)
            {
                yield return date;
            }
        }
    }

    private static IEnumerable<DateOnly> YearStarts(DateOnly first, DateOnly last)
    {
        for (var year = first.Year; year <= last.Year; year++)
        {
            var date = new DateOnly(year, 1, 1);

            if (date >= first)
            {
                yield return date;
            }
        }
    }

    private static int MonthIndex(DateOnly date) => (date.Year * MonthsPerYear) + date.Month - 1;
}
