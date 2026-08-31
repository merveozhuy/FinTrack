using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Dashboard.Dtos;

public record DashboardDto(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance,
    decimal? IncomeChangePercent,
    decimal? ExpenseChangePercent,
    IReadOnlyList<CategoryBreakdownDto> ExpenseByCategory,
    IReadOnlyList<CategoryBreakdownDto> TopExpenseCategories,
    IReadOnlyList<DailyPointDto> DailySpendingTrend,
    IReadOnlyList<RecentTransactionDto> RecentTransactions,
    IReadOnlyList<BudgetStatusDto> Budgets,
    IReadOnlyList<UpcomingPaymentDto> UpcomingPayments);

public record CategoryBreakdownDto(string CategoryName, decimal Amount, decimal Percentage);

public record DailyPointDto(DateOnly Date, decimal Amount);

public record RecentTransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string CategoryName,
    DateOnly TransactionDate,
    string? Description);

public record BudgetStatusDto(
    string CategoryName,
    decimal Limit,
    decimal Spent,
    decimal Remaining,
    decimal UsagePercentage,
    BudgetStatus Status);

public record UpcomingPaymentDto(
    string? Description,
    decimal Amount,
    string CategoryName,
    DateOnly NextExecutionDate,
    RecurrenceFrequency Frequency);
