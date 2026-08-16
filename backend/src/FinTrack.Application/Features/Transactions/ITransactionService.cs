using FinTrack.Application.Common.Models;
using FinTrack.Application.Features.Transactions.Dtos;

namespace FinTrack.Application.Features.Transactions;

public interface ITransactionService
{
    Task<PagedResult<TransactionDto>> GetAsync(TransactionQuery query, CancellationToken cancellationToken);
    Task<TransactionDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request, CancellationToken cancellationToken);
    Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
