using FinTrack.Application.Common.Security;
using FinTrack.Domain.Entities;

namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Issues signed JWT access tokens and opaque refresh tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);

    /// <summary>Returns a cryptographically random, URL-safe refresh token (raw value).</summary>
    string GenerateRefreshToken();
}
