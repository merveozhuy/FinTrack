using FinTrack.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FinTrack.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2-based hasher behind the Application abstraction.
/// A shared dummy "user" object is used because the hasher's generic parameter is only a
/// context marker and does not affect the produced hash.
/// </summary>
public class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly object HashContext = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashContext, password);

    public bool Verify(string passwordHash, string providedPassword) =>
        _hasher.VerifyHashedPassword(HashContext, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
