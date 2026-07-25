namespace FinTrack.Application.Common.Security;

/// <summary>
/// JWT configuration bound from the "Jwt" section. The <see cref="Secret"/> is supplied
/// via user-secrets / environment variables and is never committed to the repository.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
