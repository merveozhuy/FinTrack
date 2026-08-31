using FinTrack.Application.Features.Budgets.Dtos;

namespace FinTrack.Application.Features.Budgets;

public interface IBudgetService
{
    Task<IReadOnlyList<BudgetDto>> GetForMonthAsync(int year, int month, CancellationToken cancellationToken);
    Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken);
    Task<BudgetDto> UpdateAsync(Guid id, UpdateBudgetRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
