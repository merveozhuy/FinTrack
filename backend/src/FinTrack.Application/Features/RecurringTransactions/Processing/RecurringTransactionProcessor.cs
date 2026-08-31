using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Features.RecurringTransactions.Processing;

/// <summary>
/// Turns due recurring rules into concrete transactions. Idempotency comes from advancing
/// each rule's <c>NextExecutionDate</c> past the created occurrence and persisting it: a rule
/// is never processed for the same date twice, even if the worker runs again in the same day.
/// </summary>
public class RecurringTransactionProcessor : IRecurringTransactionProcessor
{
    private readonly IAppDbContext _db;

    public RecurringTransactionProcessor(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<int> ProcessDueAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var dueRules = await _db.RecurringTransactions
            .Where(r => r.IsActive && r.NextExecutionDate <= today)
            .ToListAsync(cancellationToken);

        var created = 0;

        foreach (var rule in dueRules)
        {
            // Catch up on every occurrence that is due (handles the app being offline for a while).
            while (rule.IsActive && rule.NextExecutionDate <= today)
            {
                if (rule.EndDate is { } endDate && rule.NextExecutionDate > endDate)
                {
                    rule.IsActive = false;
                    break;
                }

                _db.Transactions.Add(new Transaction
                {
                    UserId = rule.UserId,
                    Type = rule.Type,
                    Amount = rule.Amount,
                    Currency = rule.Currency,
                    Description = rule.Description,
                    CategoryId = rule.CategoryId,
                    TransactionDate = rule.NextExecutionDate
                });

                rule.LastExecutedDate = rule.NextExecutionDate;
                rule.NextExecutionDate = RecurrenceCalculator.Next(rule.NextExecutionDate, rule.Frequency);
                created++;
            }

            // Deactivate a rule that has passed its end date.
            if (rule.EndDate is { } end && rule.NextExecutionDate > end)
            {
                rule.IsActive = false;
            }
        }

        if (created > 0 || dueRules.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return created;
    }
}
