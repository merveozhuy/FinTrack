using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.RecurringTransactions.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = FinTrack.Domain.Exceptions.ValidationException;

namespace FinTrack.Application.Features.RecurringTransactions;

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateRecurringTransactionRequest> _createValidator;
    private readonly IValidator<UpdateRecurringTransactionRequest> _updateValidator;

    public RecurringTransactionService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IValidator<CreateRecurringTransactionRequest> createValidator,
        IValidator<UpdateRecurringTransactionRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        return await _db.RecurringTransactions.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.NextExecutionDate)
            .Select(r => new RecurringTransactionDto(
                r.Id, r.Type, r.Amount, r.Currency, r.CategoryId, r.Category!.Name, r.Description,
                r.Frequency, r.StartDate, r.NextExecutionDate, r.EndDate, r.LastExecutedDate, r.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();
        var category = await GetOwnedCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);

        var rule = new RecurringTransaction
        {
            UserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            CategoryId = category.Id,
            Description = request.Description?.Trim(),
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            NextExecutionDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        _db.RecurringTransactions.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(rule, category.Name);
    }

    public async Task<RecurringTransactionDto> UpdateAsync(Guid id, UpdateRecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();

        var rule = await _db.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(RecurringTransaction), id);

        var category = await GetOwnedCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);

        rule.Type = request.Type;
        rule.Amount = request.Amount;
        rule.Currency = request.Currency.Trim().ToUpperInvariant();
        rule.CategoryId = category.Id;
        rule.Description = request.Description?.Trim();
        rule.Frequency = request.Frequency;
        rule.StartDate = request.StartDate;
        rule.EndDate = request.EndDate;

        // If the rule has not run yet, re-anchor its schedule to the (possibly new) start date.
        if (rule.LastExecutedDate is null)
        {
            rule.NextExecutionDate = request.StartDate;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(rule, category.Name);
    }

    public async Task<RecurringTransactionDto> UpdateStatusAsync(Guid id, UpdateRecurringStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var rule = await _db.RecurringTransactions
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(RecurringTransaction), id);

        // When reactivating, avoid a backfill flood by not scheduling occurrences in the past.
        if (request.IsActive && !rule.IsActive)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (rule.NextExecutionDate < today)
            {
                rule.NextExecutionDate = today;
            }
        }

        rule.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(rule, rule.Category!.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var rule = await _db.RecurringTransactions
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(RecurringTransaction), id);

        _db.RecurringTransactions.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> GetOwnedCategoryAsync(
        Guid userId, Guid categoryId, TransactionType type, CancellationToken cancellationToken)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserId == userId && !c.IsArchived, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = new[] { "Category not found or not accessible." }
            });
        }

        var expected = type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense;
        if (category.Type != expected)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = new[] { $"Category type must match the transaction type ({type})." }
            });
        }

        return category;
    }

    private static RecurringTransactionDto ToDto(RecurringTransaction r, string categoryName) => new(
        r.Id, r.Type, r.Amount, r.Currency, r.CategoryId, categoryName, r.Description,
        r.Frequency, r.StartDate, r.NextExecutionDate, r.EndDate, r.LastExecutedDate, r.IsActive);
}
