using FinTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FinTrack.IntegrationTests;

/// <summary>
/// Boots the full API against a throwaway PostgreSQL (with pgvector) started by Testcontainers.
/// The real database is used so ownership filters, unique constraints and EF queries are exercised
/// exactly as in production. Configuration (connection string, JWT secret) is injected here so no
/// user-secrets or external services are required.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .WithDatabase("fintrack_test")
        .WithUsername("fintrack")
        .WithPassword("fintrack")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-tests-signing-secret-key-1234567890-abcdef");
        builder.UseSetting("Jwt:Issuer", "fintrack");
        builder.UseSetting("Jwt:Audience", "fintrack-client");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");

        // Effectively disable auth rate limiting during tests (many registrations per run).
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "1000000");

        // Disable the recurring-transaction background worker so tests can drive the processor
        // deterministically instead of racing a timer.
        builder.UseSetting("RecurringTransactions:Enabled", "false");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
