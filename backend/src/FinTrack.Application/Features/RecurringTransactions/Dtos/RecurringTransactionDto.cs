using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.RecurringTransactions.Dtos;

public record RecurringTransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Currency,
    Guid CategoryId,
    string CategoryName,
    string? Description,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly NextExecutionDate,
    DateOnly? EndDate,
    DateOnly? LastExecutedDate,
    bool IsActive);
