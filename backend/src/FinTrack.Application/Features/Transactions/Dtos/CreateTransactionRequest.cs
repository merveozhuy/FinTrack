using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Transactions.Dtos;

public class CreateTransactionRequest
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>Optional credit card the expense was paid with (ignored for income).</summary>
    public Guid? CreditCardId { get; set; }

    public DateOnly TransactionDate { get; set; }
}
