using FinTrack.Application.Features.Dashboard.Dtos;

namespace FinTrack.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int year, int month, CancellationToken cancellationToken);
}
