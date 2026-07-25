namespace FinTrack.Application.Common.Security;

public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
