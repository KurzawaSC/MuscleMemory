namespace MuscleMemory.Charting;

public sealed record DateTicks(IReadOnlyList<DateOnly> Dates, DateTickUnit Unit);
