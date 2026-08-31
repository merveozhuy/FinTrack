namespace FinTrack.Application.Features.CreditCards.Dtos;

public record CreditCardDto(
    Guid Id,
    string Name,
    string? Last4,
    decimal? CreditLimit,
    decimal CurrentDebt,
    decimal? AvailableLimit,
    decimal? UsagePercentage);
