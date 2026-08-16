using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinTrack.Application.Features.Auth.Dtos;
using FinTrack.Application.Features.Categories.Dtos;

namespace FinTrack.IntegrationTests;

public static class ApiTestExtensions
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Registers a fresh user and returns an HttpClient with its bearer token attached.</summary>
    public static async Task<HttpClient> RegisterClientAsync(this ApiFactory factory, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= $"user-{Guid.NewGuid():N}@fintrack.local";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!", displayName = "Test User" });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    /// <summary>Finds the id of a (default) category by name for the authenticated user.</summary>
    public static async Task<Guid> GetCategoryIdAsync(this HttpClient client, string name)
    {
        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", Json);
        return categories!.First(c => c.Name == name).Id;
    }
}
