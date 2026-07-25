using System.Security.Cryptography;
using System.Text;

namespace FinTrack.Application.Common.Security;

/// <summary>
/// Hashes refresh tokens before persistence. Only the hash is stored, so a database leak
/// does not expose usable refresh tokens. SHA-256 is sufficient here because the raw token
/// is already a high-entropy random value (unlike a user password).
/// </summary>
public static class TokenHasher
{
    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
