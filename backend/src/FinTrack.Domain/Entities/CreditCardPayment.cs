using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

/// <summary>A payment made towards a credit card, which reduces its outstanding debt.</summary>
public class CreditCardPayment : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }

    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
}
