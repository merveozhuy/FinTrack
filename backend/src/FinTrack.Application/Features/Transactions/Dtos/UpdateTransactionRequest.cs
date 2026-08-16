using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Transactions.Dtos;

public class UpdateTransactionRequest
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public DateOnly TransactionDate { get; set; }
}
