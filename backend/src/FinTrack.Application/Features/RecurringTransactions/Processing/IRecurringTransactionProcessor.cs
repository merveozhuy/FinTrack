namespace FinTrack.Application.Features.RecurringTransactions.Processing;

public interface IRecurringTransactionProcessor
{
    /// <summary>
    /// Materializes all due occurrences (NextExecutionDate on or before <paramref name="today"/>)
    /// into real transactions and advances each rule. Returns the number of transactions created.
    /// </summary>
    Task<int> ProcessDueAsync(DateOnly today, CancellationToken cancellationToken);
}
