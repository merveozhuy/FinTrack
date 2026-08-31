using FinTrack.Application.Features.RecurringTransactions.Dtos;

namespace FinTrack.Application.Features.RecurringTransactions;

public interface IRecurringTransactionService
{
    Task<IReadOnlyList<RecurringTransactionDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<RecurringTransactionDto> CreateAsync(CreateRecurringTransactionRequest request, CancellationToken cancellationToken);
    Task<RecurringTransactionDto> UpdateAsync(Guid id, UpdateRecurringTransactionRequest request, CancellationToken cancellationToken);
    Task<RecurringTransactionDto> UpdateStatusAsync(Guid id, UpdateRecurringStatusRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
