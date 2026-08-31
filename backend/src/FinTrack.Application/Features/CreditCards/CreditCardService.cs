using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.CreditCards.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Features.CreditCards;

public class CreditCardService : ICreditCardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateCreditCardRequest> _createValidator;
    private readonly IValidator<UpdateCreditCardRequest> _updateValidator;
    private readonly IValidator<CreateCardPaymentRequest> _paymentValidator;

    public CreditCardService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IValidator<CreateCreditCardRequest> createValidator,
        IValidator<UpdateCreditCardRequest> updateValidator,
        IValidator<CreateCardPaymentRequest> paymentValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _paymentValidator = paymentValidator;
    }

    public async Task<IReadOnlyList<CreditCardDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var cards = await _db.CreditCards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Last4, c.CreditLimit })
            .ToListAsync(cancellationToken);

        if (cards.Count == 0)
        {
            return Array.Empty<CreditCardDto>();
        }

        var spentByCard = await GetSpentByCardAsync(userId, cancellationToken);
        var paidByCard = await GetPaidByCardAsync(userId, cancellationToken);

        return cards
            .Select(c =>
            {
                var debt = spentByCard.GetValueOrDefault(c.Id, 0m) - paidByCard.GetValueOrDefault(c.Id, 0m);
                return ToDto(c.Id, c.Name, c.Last4, c.CreditLimit, debt);
            })
            .ToList();
    }

    public async Task<CreditCardDto> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);
        var userId = _currentUser.RequireUserId();

        var card = new CreditCard
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Last4 = string.IsNullOrWhiteSpace(request.Last4) ? null : request.Last4.Trim(),
            CreditLimit = request.CreditLimit,
        };

        _db.CreditCards.Add(card);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(card.Id, card.Name, card.Last4, card.CreditLimit, 0m);
    }

    public async Task<CreditCardDto> UpdateAsync(Guid id, UpdateCreditCardRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);
        var userId = _currentUser.RequireUserId();

        var card = await _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CreditCard), id);

        card.Name = request.Name.Trim();
        card.Last4 = string.IsNullOrWhiteSpace(request.Last4) ? null : request.Last4.Trim();
        card.CreditLimit = request.CreditLimit;
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(userId, card, cancellationToken);
    }

    public async Task<CreditCardDto> AddPaymentAsync(Guid id, CreateCardPaymentRequest request, CancellationToken cancellationToken)
    {
        await _paymentValidator.EnsureValidAsync(request, cancellationToken);
        var userId = _currentUser.RequireUserId();

        var card = await _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CreditCard), id);

        _db.CreditCardPayments.Add(new CreditCardPayment
        {
            UserId = userId,
            CreditCardId = card.Id,
            Amount = request.Amount,
            PaymentDate = request.PaymentDate,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(userId, card, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        var card = await _db.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CreditCard), id);

        // Transactions are unlinked (SetNull) and payments are removed (cascade) by the database.
        _db.CreditCards.Remove(card);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, decimal>> GetSpentByCardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var grouped = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.CreditCardId != null)
            .GroupBy(t => t.CreditCardId!.Value)
            .Select(g => new { CardId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(x => x.CardId, x => x.Amount);
    }

    private async Task<Dictionary<Guid, decimal>> GetPaidByCardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var grouped = await _db.CreditCardPayments.AsNoTracking()
            .Where(p => p.UserId == userId)
            .GroupBy(p => p.CreditCardId)
            .Select(g => new { CardId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(x => x.CardId, x => x.Amount);
    }

    private async Task<CreditCardDto> BuildDtoAsync(Guid userId, CreditCard card, CancellationToken cancellationToken)
    {
        var spent = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.CreditCardId == card.Id)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
        var paid = await _db.CreditCardPayments.AsNoTracking()
            .Where(p => p.UserId == userId && p.CreditCardId == card.Id)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        return ToDto(card.Id, card.Name, card.Last4, card.CreditLimit, spent - paid);
    }

    private static CreditCardDto ToDto(Guid id, string name, string? last4, decimal? creditLimit, decimal debt)
    {
        decimal? available = creditLimit.HasValue ? creditLimit.Value - debt : null;
        decimal? usage = creditLimit is > 0 ? Math.Round(debt / creditLimit.Value * 100m, 2) : null;
        return new CreditCardDto(id, name, last4, creditLimit, debt, available, usage);
    }
}
