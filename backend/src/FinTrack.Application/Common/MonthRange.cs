namespace FinTrack.Application.Common;

/// <summary>The inclusive first and last calendar day of a given year/month.</summary>
public readonly record struct MonthRange(DateOnly Start, DateOnly End)
{
    public static MonthRange For(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new MonthRange(start, end);
    }
}
