using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

public class CreditCard : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Last four digits, for display only. The full number is never stored.</summary>
    public string? Last4 { get; set; }

    /// <summary>Optional total credit limit, used to show remaining available credit.</summary>
    public decimal? CreditLimit { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<CreditCardPayment> Payments { get; set; } = new List<CreditCardPayment>();
}
