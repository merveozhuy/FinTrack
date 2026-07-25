using FinTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Liveness probe that also reports whether the database is reachable.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        bool databaseReachable;
        try
        {
            databaseReachable = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            // Health must never throw; an unreachable database is a reported state, not an error.
            databaseReachable = false;
        }

        return Ok(new
        {
            status = "ok",
            database = databaseReachable ? "connected" : "unreachable",
            timestampUtc = DateTime.UtcNow
        });
    }
}
