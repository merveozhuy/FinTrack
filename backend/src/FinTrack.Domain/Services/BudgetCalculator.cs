using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Services;

/// <summary>Result of evaluating spending against a monthly budget limit.</summary>
public record BudgetCalculation(
    decimal Limit,
    decimal Spent,
    decimal Remaining,
    decimal UsagePercentage,
    BudgetStatus Status,
    bool IsThresholdReached);

/// <summary>
/// Pure budget math, kept in the domain and free of persistence so it can be unit tested directly.
/// This is the kind of calculation the RAG assistant will later explain but never compute itself.
/// </summary>
public static class BudgetCalculator
{
    public const decimal WarningThresholdPercent = 80m;

    public static BudgetCalculation Calculate(decimal monthlyLimit, decimal spent)
    {
        var remaining = monthlyLimit - spent;
        var usagePercentage = monthlyLimit <= 0m ? 0m : Math.Round(spent / monthlyLimit * 100m, 2);

        var status = spent > monthlyLimit
            ? BudgetStatus.Exceeded
            : usagePercentage >= WarningThresholdPercent
                ? BudgetStatus.Warning
                : BudgetStatus.Ok;

        var isThresholdReached = usagePercentage >= WarningThresholdPercent;

        return new BudgetCalculation(monthlyLimit, spent, remaining, usagePercentage, status, isThresholdReached);
    }
}
