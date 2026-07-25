using FinTrack.Application.Features.Auth.Dtos;

namespace FinTrack.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken);
}
