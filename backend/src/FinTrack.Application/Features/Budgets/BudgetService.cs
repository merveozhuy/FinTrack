using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.Budgets.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FinTrack.Domain.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = FinTrack.Domain.Exceptions.ValidationException;

namespace FinTrack.Application.Features.Budgets;

public class BudgetService : IBudgetService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateBudgetRequest> _createValidator;
    private readonly IValidator<UpdateBudgetRequest> _updateValidator;

    public BudgetService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IValidator<CreateBudgetRequest> createValidator,
        IValidator<UpdateBudgetRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<BudgetDto>> GetForMonthAsync(int year, int month, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var budgets = await _db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new { b.Id, b.CategoryId, CategoryName = b.Category!.Name, b.Year, b.Month, b.MonthlyLimit })
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            return Array.Empty<BudgetDto>();
        }

        var spentByCategory = await GetSpentByCategoryAsync(userId, year, month, cancellationToken);

        return budgets
            .Select(b =>
            {
                var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0m);
                var calc = BudgetCalculator.Calculate(b.MonthlyLimit, spent);
                return new BudgetDto(b.Id, b.CategoryId, b.CategoryName, b.Year, b.Month,
                    calc.Limit, calc.Spent, calc.Remaining, calc.UsagePercentage, calc.Status, calc.IsThresholdReached);
            })
            .OrderByDescending(b => b.UsagePercentage)
            .ToList();
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId && !c.IsArchived, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = new[] { "Category not found or not accessible." }
            });
        }

        if (category.Type != CategoryType.Expense)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = new[] { "Budgets can only be set for expense categories." }
            });
        }

        var duplicate = await _db.Budgets.AnyAsync(
            b => b.UserId == userId && b.CategoryId == request.CategoryId && b.Year == request.Year && b.Month == request.Month,
            cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("A budget for this category and month already exists.");
        }

        var budget = new Budget
        {
            UserId = userId,
            CategoryId = category.Id,
            Year = request.Year,
            Month = request.Month,
            MonthlyLimit = request.MonthlyLimit
        };

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(budget, category.Name, cancellationToken);
    }

    public async Task<BudgetDto> UpdateAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();

        var budget = await _db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), id);

        budget.MonthlyLimit = request.MonthlyLimit;
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(budget, budget.Category!.Name, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), id);

        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> GetSpentByCategoryAsync(
        Guid userId, int year, int month, CancellationToken cancellationToken)
    {
        var range = MonthRange.For(year, month);

        var grouped = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= range.Start
                        && t.TransactionDate <= range.End)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(x => x.CategoryId, x => x.Amount);
    }

    private async Task<BudgetDto> BuildDtoAsync(Budget budget, string categoryName, CancellationToken cancellationToken)
    {
        var spentByCategory = await GetSpentByCategoryAsync(budget.UserId, budget.Year, budget.Month, cancellationToken);
        var spent = spentByCategory.GetValueOrDefault(budget.CategoryId, 0m);
        var calc = BudgetCalculator.Calculate(budget.MonthlyLimit, spent);

        return new BudgetDto(budget.Id, budget.CategoryId, categoryName, budget.Year, budget.Month,
            calc.Limit, calc.Spent, calc.Remaining, calc.UsagePercentage, calc.Status, calc.IsThresholdReached);
    }
}
