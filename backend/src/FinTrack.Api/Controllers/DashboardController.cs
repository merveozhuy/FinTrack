using FinTrack.Application.Features.Dashboard;
using FinTrack.Application.Features.Dashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Returns an aggregated financial overview for the given month in a single request.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardDto>> Get(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetAsync(year, month, cancellationToken);
        return Ok(dashboard);
    }
}
