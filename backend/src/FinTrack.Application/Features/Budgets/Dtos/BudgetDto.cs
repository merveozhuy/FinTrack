using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Budgets.Dtos;

public record BudgetDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    int Year,
    int Month,
    decimal MonthlyLimit,
    decimal Spent,
    decimal Remaining,
    decimal UsagePercentage,
    BudgetStatus Status,
    bool IsThresholdReached);
