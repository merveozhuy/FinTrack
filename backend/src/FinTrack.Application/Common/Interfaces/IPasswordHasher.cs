namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Hashes and verifies user passwords. Implemented in Infrastructure so the Application
/// layer stays free of a specific hashing library.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
