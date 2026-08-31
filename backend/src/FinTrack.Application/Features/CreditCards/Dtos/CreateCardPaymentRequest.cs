namespace FinTrack.Application.Features.CreditCards.Dtos;

public class CreateCardPaymentRequest
{
    public decimal Amount { get; set; }
    public DateOnly PaymentDate { get; set; }
}
