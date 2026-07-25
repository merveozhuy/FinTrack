using FinTrack.Application.Common.Security;
using FluentAssertions;

namespace FinTrack.UnitTests.Security;

public class TokenHasherTests
{
    [Fact]
    public void Hash_WhenSameInput_ShouldBeDeterministic()
    {
        var first = TokenHasher.Hash("refresh-token-value");
        var second = TokenHasher.Hash("refresh-token-value");

        first.Should().Be(second);
    }

    [Fact]
    public void Hash_WhenDifferentInput_ShouldProduceDifferentHash()
    {
        var first = TokenHasher.Hash("token-a");
        var second = TokenHasher.Hash("token-b");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Hash_WhenCalled_ShouldNotReturnRawToken()
    {
        const string raw = "refresh-token-value";

        TokenHasher.Hash(raw).Should().NotBe(raw);
    }
}
