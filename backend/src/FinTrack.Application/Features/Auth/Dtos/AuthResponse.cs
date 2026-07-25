namespace FinTrack.Application.Features.Auth.Dtos;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User);
