using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.RecurringTransactions.Dtos;

public class UpdateRecurringTransactionRequest
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public Guid CategoryId { get; set; }
    public string? Description { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
