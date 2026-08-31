using FinTrack.Application.Features.CreditCards.Dtos;

namespace FinTrack.Application.Features.CreditCards;

public interface ICreditCardService
{
    Task<IReadOnlyList<CreditCardDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CreditCardDto> CreateAsync(CreateCreditCardRequest request, CancellationToken cancellationToken);
    Task<CreditCardDto> UpdateAsync(Guid id, UpdateCreditCardRequest request, CancellationToken cancellationToken);
    Task<CreditCardDto> AddPaymentAsync(Guid id, CreateCardPaymentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
