using FinTrack.Application.Common.Interfaces;
using FinTrack.Infrastructure.Ai;
using FinTrack.Infrastructure.Persistence;
using FinTrack.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        // Expose the same scoped AppDbContext instance through the Application-layer abstraction.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        AddAiProviders(services, configuration);

        return services;
    }

    private static void AddAiProviders(IServiceCollection services, IConfiguration configuration)
    {
        // Semantic search always runs against our own pgvector store.
        services.AddScoped<ISemanticSearch, PgVectorSemanticSearch>();

        var provider = configuration["Ai:Provider"] ?? "Fake";
        var apiKey = configuration["OpenAI:ApiKey"];

        if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.AddHttpClient();
            services.AddSingleton(new AiOptions
            {
                Provider = "OpenAI",
                ApiKey = apiKey,
                ChatModel = configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini",
                EmbeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small",
                BaseUrl = configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1",
            });
            services.AddScoped<IEmbeddingProvider, OpenAiEmbeddingProvider>();
            services.AddScoped<ILlmProvider, OpenAiLlmProvider>();
        }
        else
        {
            // Default: deterministic, key-free providers so the app runs anywhere.
            services.AddScoped<IEmbeddingProvider, FakeEmbeddingProvider>();
            services.AddScoped<ILlmProvider, FakeLlmProvider>();
        }
    }
}
