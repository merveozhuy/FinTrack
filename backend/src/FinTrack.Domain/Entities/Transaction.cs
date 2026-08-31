using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Transaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public TransactionType Type { get; set; }

    /// <summary>Monetary amount. Always stored as decimal to avoid floating point rounding.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code. TRY for the first release; the model allows others.</summary>
    public string Currency { get; set; } = "TRY";

    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>Optional credit card an expense was paid with. Null for cash/bank transactions and income.</summary>
    public Guid? CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }

    /// <summary>Calendar day the transaction occurred (no time component).</summary>
    public DateOnly TransactionDate { get; set; }
}
