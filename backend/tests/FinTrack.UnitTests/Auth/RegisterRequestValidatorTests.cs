using FinTrack.Application.Features.Auth.Dtos;
using FinTrack.Application.Features.Auth.Validators;
using FluentAssertions;

namespace FinTrack.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest ValidRequest() => new()
    {
        Email = "demo@fintrack.local",
        Password = "Sup3rSecret!",
        DisplayName = "Demo User"
    };

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WhenEmailIsInvalid_ShouldFail(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Validate_WhenPasswordTooShort_ShouldFail()
    {
        var request = ValidRequest();
        request.Password = "short";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Validate_WhenDisplayNameIsEmpty_ShouldFail()
    {
        var request = ValidRequest();
        request.DisplayName = string.Empty;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.DisplayName));
    }
}
