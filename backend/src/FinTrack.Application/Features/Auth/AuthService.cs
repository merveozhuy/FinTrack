using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Common.Security;
using FinTrack.Application.Common.Validation;
using FinTrack.Application.Features.Auth.Dtos;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinTrack.Application.Features.Auth;

public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        ICurrentUser currentUser,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _currentUser = currentUser;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await _registerValidator.EnsureValidAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        _db.Users.Add(user);
        _db.Categories.AddRange(DefaultCategories.CreateFor(user.Id));

        var refreshToken = await IssueRefreshTokenAsync(user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await _loginValidator.EnsureValidAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Same error whether the user is missing or the password is wrong, so we do not
        // leak which emails are registered.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException(InvalidCredentialsMessage);
        }

        var refreshToken = await IssueRefreshTokenAsync(user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var tokenHash = TokenHasher.Hash(request.RefreshToken);

        var storedToken = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Rotate: revoke the used token and issue a fresh one.
        storedToken.RevokedAt = DateTime.UtcNow;
        var newRefreshToken = await IssueRefreshTokenAsync(storedToken.UserId, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(storedToken.User, newRefreshToken);
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("The request is not authenticated.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return new UserDto(user.Id, user.Email, user.DisplayName);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rawToken = _tokenGenerator.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = TokenHasher.Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });

        // Caller performs SaveChanges so registration/login stays a single transaction.
        await Task.CompletedTask;
        return rawToken;
    }

    private AuthResponse BuildAuthResponse(User user, string refreshToken)
    {
        var accessToken = _tokenGenerator.GenerateAccessToken(user);
        var userDto = new UserDto(user.Id, user.Email, user.DisplayName);
        return new AuthResponse(accessToken.Token, refreshToken, accessToken.ExpiresAtUtc, userDto);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
