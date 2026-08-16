using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.Auth.Dtos;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthEndpointsTests
{
    private readonly ApiFactory _factory;

    public AuthEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ThenGetMe_ReturnsCurrentUser()
    {
        var email = $"me-{Guid.NewGuid():N}@fintrack.local";
        var client = await _factory.RegisterClientAsync(email);

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me", ApiTestExtensions.Json);

        me.Should().NotBeNull();
        me!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@fintrack.local";
        var client = _factory.CreateClient();
        var payload = new { email, password = "Sup3rSecret!", displayName = "Test User" };

        var first = await client.PostAsJsonAsync("/api/auth/register", payload);
        var second = await client.PostAsJsonAsync("/api/auth/register", payload);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
