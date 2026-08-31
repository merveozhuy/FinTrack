using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Services;

/// <summary>
/// Computes the next execution date for a recurring rule. Pure and side-effect free so the
/// background worker's scheduling can be unit tested without a database or a clock.
/// </summary>
public static class RecurrenceCalculator
{
    public static DateOnly Next(DateOnly from, RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Weekly => from.AddDays(7),
        RecurrenceFrequency.Monthly => from.AddMonths(1),
        RecurrenceFrequency.Yearly => from.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported recurrence frequency.")
    };
}
