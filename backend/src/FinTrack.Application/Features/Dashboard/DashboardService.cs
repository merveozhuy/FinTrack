using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Features.Dashboard.Dtos;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;
using ValidationException = FinTrack.Domain.Exceptions.ValidationException;

namespace FinTrack.Application.Features.Dashboard;

public class DashboardService : IDashboardService
{
    private const int RecentTransactionCount = 5;
    private const int TopCategoryCount = 5;
    private const int UpcomingHorizonDays = 30;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DashboardService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["month"] = new[] { "A valid year (2000-2100) and month (1-12) are required." }
            });
        }

        var userId = _currentUser.RequireUserId();
        var range = MonthRange.For(year, month);
        var previous = MonthRange.For(year, month).Start.AddMonths(-1);
        var previousRange = MonthRange.For(previous.Year, previous.Month);

        var totalIncome = await SumAsync(userId, TransactionType.Income, range, cancellationToken);
        var totalExpense = await SumAsync(userId, TransactionType.Expense, range, cancellationToken);
        var previousIncome = await SumAsync(userId, TransactionType.Income, previousRange, cancellationToken);
        var previousExpense = await SumAsync(userId, TransactionType.Expense, previousRange, cancellationToken);

        var expenseByCategory = await GetExpenseByCategoryAsync(userId, range, totalExpense, cancellationToken);
        var dailyTrend = await GetDailyTrendAsync(userId, range, cancellationToken);
        var recent = await GetRecentTransactionsAsync(userId, cancellationToken);
        var budgets = await GetBudgetStatusAsync(userId, year, month, cancellationToken);
        var upcoming = await GetUpcomingPaymentsAsync(userId, cancellationToken);

        return new DashboardDto(
            year,
            month,
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            ChangePercent(totalIncome, previousIncome),
            ChangePercent(totalExpense, previousExpense),
            expenseByCategory,
            expenseByCategory.Take(TopCategoryCount).ToList(),
            dailyTrend,
            recent,
            budgets,
            upcoming);
    }

    private async Task<decimal> SumAsync(Guid userId, TransactionType type, MonthRange range, CancellationToken cancellationToken)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == type
                        && t.TransactionDate >= range.Start && t.TransactionDate <= range.End)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
    }

    private static decimal? ChangePercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            // No baseline to compare against; the client shows this as "new" rather than a percentage.
            return null;
        }

        return Math.Round((current - previous) / previous * 100m, 2);
    }

    private async Task<List<CategoryBreakdownDto>> GetExpenseByCategoryAsync(
        Guid userId, MonthRange range, decimal totalExpense, CancellationToken cancellationToken)
    {
        var groups = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                        && t.TransactionDate >= range.Start && t.TransactionDate <= range.End)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new { CategoryName = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return groups
            .OrderByDescending(g => g.Amount)
            .Select(g => new CategoryBreakdownDto(
                g.CategoryName,
                g.Amount,
                totalExpense <= 0m ? 0m : Math.Round(g.Amount / totalExpense * 100m, 2)))
            .ToList();
    }

    private async Task<List<DailyPointDto>> GetDailyTrendAsync(
        Guid userId, MonthRange range, CancellationToken cancellationToken)
    {
        var points = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                        && t.TransactionDate >= range.Start && t.TransactionDate <= range.End)
            .GroupBy(t => t.TransactionDate)
            .Select(g => new DailyPointDto(g.Key, g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);

        return points.OrderBy(p => p.Date).ToList();
    }

    private async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAt)
            .Take(RecentTransactionCount)
            .Select(t => new RecentTransactionDto(t.Id, t.Type, t.Amount, t.Category!.Name, t.TransactionDate, t.Description))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<BudgetStatusDto>> GetBudgetStatusAsync(
        Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var budgets = await _db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new { b.CategoryId, CategoryName = b.Category!.Name, b.MonthlyLimit })
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return new List<BudgetStatusDto>();
        }

        var range = MonthRange.For(year, month);
        var spentByCategory = (await _db.Transactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.Type == TransactionType.Expense
                            && t.TransactionDate >= range.Start && t.TransactionDate <= range.End)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.CategoryId, x => x.Amount);

        return budgets
            .Select(b =>
            {
                var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0m);
                var calc = BudgetCalculator.Calculate(b.MonthlyLimit, spent);
                return new BudgetStatusDto(b.CategoryName, calc.Limit, calc.Spent, calc.Remaining, calc.UsagePercentage, calc.Status);
            })
            .OrderByDescending(b => b.UsagePercentage)
            .ToList();
    }

    private async Task<List<UpcomingPaymentDto>> GetUpcomingPaymentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(UpcomingHorizonDays);

        return await _db.RecurringTransactions.AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive
                        && r.NextExecutionDate >= today && r.NextExecutionDate <= horizon)
            .OrderBy(r => r.NextExecutionDate)
            .Select(r => new UpcomingPaymentDto(r.Description, r.Amount, r.Category!.Name, r.NextExecutionDate, r.Frequency))
            .ToListAsync(cancellationToken);
    }
}
