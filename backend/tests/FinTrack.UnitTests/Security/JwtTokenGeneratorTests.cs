using System.IdentityModel.Tokens.Jwt;
using FinTrack.Application.Common.Security;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FinTrack.UnitTests.Security;

public class JwtTokenGeneratorTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "unit-test-secret-key-that-is-long-enough-1234567890",
        Issuer = "fintrack-test",
        Audience = "fintrack-test-client",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    private readonly JwtTokenGenerator _generator = new(Options.Create(Settings));

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserIdAsSubjectClaim()
    {
        var user = new User { Email = "demo@fintrack.local", DisplayName = "Demo" };

        var result = _generator.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void GenerateAccessToken_ShouldSetExpiryAccordingToSettings()
    {
        var user = new User { Email = "demo@fintrack.local", DisplayName = "Demo" };
        var before = DateTime.UtcNow;

        var result = _generator.GenerateAccessToken(user);

        result.ExpiresAtUtc.Should().BeCloseTo(before.AddMinutes(Settings.AccessTokenMinutes), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GenerateAccessToken_ShouldUseConfiguredIssuerAndAudience()
    {
        var user = new User { Email = "demo@fintrack.local", DisplayName = "Demo" };

        var result = _generator.GenerateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        token.Issuer.Should().Be(Settings.Issuer);
        token.Audiences.Should().Contain(Settings.Audience);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueValues()
    {
        var first = _generator.GenerateRefreshToken();
        var second = _generator.GenerateRefreshToken();

        first.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
    }
}
