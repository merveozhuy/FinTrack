using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Transactions.Dtos;

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    Guid? CreditCardId,
    string? CreditCardName,
    DateOnly TransactionDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);
