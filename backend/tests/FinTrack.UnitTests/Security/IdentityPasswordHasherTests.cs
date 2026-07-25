using FinTrack.Infrastructure.Security;
using FluentAssertions;

namespace FinTrack.UnitTests.Security;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void Hash_WhenCalled_ShouldNotReturnPlainTextPassword()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("Sup3rSecret!");
    }

    [Fact]
    public void Verify_WhenPasswordMatchesHash_ShouldReturnTrue()
    {
        const string password = "Sup3rSecret!";
        var hash = _hasher.Hash(password);

        _hasher.Verify(hash, password).Should().BeTrue();
    }

    [Fact]
    public void Verify_WhenPasswordDoesNotMatchHash_ShouldReturnFalse()
    {
        var hash = _hasher.Hash("Sup3rSecret!");

        _hasher.Verify(hash, "WrongPassword").Should().BeFalse();
    }
}
