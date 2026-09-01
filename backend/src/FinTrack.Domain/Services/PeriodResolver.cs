namespace FinTrack.Domain.Services;

/// <summary>A date range inferred from a natural-language question, with a human label.</summary>
public record ResolvedPeriod(DateOnly Start, DateOnly End, string Label, int MonthsSpan);

/// <summary>
/// Turns time expressions in a question ("geçen ay", "son 3 ay", "bu yıl", …) into a concrete
/// date range. Pure and deterministic so the assistant can answer questions about any period,
/// not just the current month. Defaults to the current month when no time expression is present.
/// </summary>
public static class PeriodResolver
{
    public static ResolvedPeriod Resolve(string question, DateOnly today)
    {
        var q = (question ?? string.Empty).ToLowerInvariant();

        var currentStart = new DateOnly(today.Year, today.Month, 1);
        var currentEnd = currentStart.AddMonths(1).AddDays(-1);

        if (Contains(q, "geçen ay", "önceki ay", "gecen ay", "last month"))
        {
            var start = currentStart.AddMonths(-1);
            return new ResolvedPeriod(start, start.AddMonths(1).AddDays(-1), "geçen ay", 1);
        }

        if (Contains(q, "geçen yıl", "gecen yil", "last year"))
        {
            return new ResolvedPeriod(new DateOnly(today.Year - 1, 1, 1), new DateOnly(today.Year - 1, 12, 31), "geçen yıl", 12);
        }

        if (Contains(q, "bu yıl", "bu yil", "this year", "yıl içinde", "yılbaşından"))
        {
            return new ResolvedPeriod(new DateOnly(today.Year, 1, 1), currentEnd, "bu yıl", today.Month);
        }

        if (Contains(q, "son 6 ay", "son altı ay", "last 6 months", "6 ay"))
        {
            return new ResolvedPeriod(currentStart.AddMonths(-5), currentEnd, "son 6 ay", 6);
        }

        if (Contains(q, "son 3 ay", "son üç ay", "son uc ay", "last 3 months", "3 ay", "üç ay"))
        {
            return new ResolvedPeriod(currentStart.AddMonths(-2), currentEnd, "son 3 ay", 3);
        }

        return new ResolvedPeriod(currentStart, currentEnd, "bu ay", 1);
    }

    private static bool Contains(string text, params string[] terms) => terms.Any(text.Contains);
}
