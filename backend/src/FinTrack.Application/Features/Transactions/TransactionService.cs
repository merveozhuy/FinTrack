using System.Linq.Expressions;
using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Models;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.Transactions.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = FinTrack.Domain.Exceptions.ValidationException;

namespace FinTrack.Application.Features.Transactions;

public class TransactionService : ITransactionService
{
    private const int MaxPageSize = 100;

    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateTransactionRequest> _createValidator;
    private readonly IValidator<UpdateTransactionRequest> _updateValidator;

    public TransactionService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IValidator<CreateTransactionRequest> createValidator,
        IValidator<UpdateTransactionRequest> updateValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<TransactionDto>> GetAsync(TransactionQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var filtered = ApplyFilters(_db.Transactions.AsNoTracking().Where(t => t.UserId == userId), query);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await ApplySorting(filtered, query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>(items, page, pageSize, totalCount);
    }

    public async Task<TransactionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        return await _db.Transactions.AsNoTracking()
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), id);
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();
        var category = await GetOwnedCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var card = await GetOwnedCardAsync(userId, request.CreditCardId, request.Type, cancellationToken);

        var transaction = new Transaction
        {
            UserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            Description = request.Description?.Trim(),
            CategoryId = category.Id,
            CreditCardId = card?.Id,
            TransactionDate = request.TransactionDate
        };

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(transaction, category.Name, card?.Name);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var userId = _currentUser.RequireUserId();

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), id);

        var category = await GetOwnedCategoryAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var card = await GetOwnedCardAsync(userId, request.CreditCardId, request.Type, cancellationToken);

        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.Currency = request.Currency.Trim().ToUpperInvariant();
        transaction.Description = request.Description?.Trim();
        transaction.CategoryId = category.Id;
        transaction.CreditCardId = card?.Id;
        transaction.TransactionDate = request.TransactionDate;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(transaction, category.Name, card?.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), id);

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionQuery filter)
    {
        if (filter.From is { } from)
        {
            query = query.Where(t => t.TransactionDate >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(t => t.TransactionDate <= to);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(t => t.CategoryId == categoryId);
        }

        if (filter.Type is { } type)
        {
            query = query.Where(t => t.Type == type);
        }

        if (filter.MinAmount is { } min)
        {
            query = query.Where(t => t.Amount >= min);
        }

        if (filter.MaxAmount is { } max)
        {
            query = query.Where(t => t.Amount <= max);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(t => t.Description != null && t.Description.ToLower().Contains(term));
        }

        return query;
    }

    private static IQueryable<Transaction> ApplySorting(IQueryable<Transaction> query, TransactionQuery filter)
    {
        // Secondary sort by Id keeps paging stable when the primary key ties.
        return (filter.SortBy, filter.SortDir) switch
        {
            (TransactionSortBy.Amount, SortDirection.Asc) => query.OrderBy(t => t.Amount).ThenBy(t => t.Id),
            (TransactionSortBy.Amount, SortDirection.Desc) => query.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Id),
            (TransactionSortBy.Date, SortDirection.Asc) => query.OrderBy(t => t.TransactionDate).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
        };
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

        // Income transactions belong to Income categories and vice versa.
        var expectedCategoryType = type == TransactionType.Income ? CategoryType.Income : CategoryType.Expense;
        if (category.Type != expectedCategoryType)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryId"] = new[] { $"Category type must match the transaction type ({type})." }
            });
        }

        return category;
    }

    private async Task<CreditCard?> GetOwnedCardAsync(
        Guid userId, Guid? cardId, TransactionType type, CancellationToken cancellationToken)
    {
        // Cards apply to expenses only; income is never linked to a card.
        if (cardId is not { } id || type != TransactionType.Expense)
        {
            return null;
        }

        return await _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["creditCardId"] = new[] { "Credit card not found or not accessible." }
            });
    }

    // Projection used inside DB queries; EF translates the navigations to joins.
    private static readonly Expression<Func<Transaction, TransactionDto>> Projection = t => new TransactionDto(
        t.Id, t.Type, t.Amount, t.Currency, t.Description,
        t.CategoryId, t.Category!.Name,
        t.CreditCardId, t.CreditCard != null ? t.CreditCard.Name : null,
        t.TransactionDate, t.CreatedAt, t.UpdatedAt);

    // In-memory form (used after create/update) with a known category and card name.
    private static TransactionDto ToDto(Transaction t, string categoryName, string? cardName) => new(
        t.Id, t.Type, t.Amount, t.Currency, t.Description,
        t.CategoryId, categoryName, t.CreditCardId, cardName,
        t.TransactionDate, t.CreatedAt, t.UpdatedAt);
}
