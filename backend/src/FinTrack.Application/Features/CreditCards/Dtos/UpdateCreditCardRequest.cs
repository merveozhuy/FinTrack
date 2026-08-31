namespace FinTrack.Application.Features.CreditCards.Dtos;

public class UpdateCreditCardRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Last4 { get; set; }
    public decimal? CreditLimit { get; set; }
}
