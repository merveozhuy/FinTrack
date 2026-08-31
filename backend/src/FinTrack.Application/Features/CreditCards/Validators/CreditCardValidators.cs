using FinTrack.Application.Features.CreditCards.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.CreditCards.Validators;

public class CreateCreditCardRequestValidator : AbstractValidator<CreateCreditCardRequest>
{
    public CreateCreditCardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Last4).Matches("^[0-9]{4}$").When(x => !string.IsNullOrEmpty(x.Last4))
            .WithMessage("Last4 must be exactly four digits.");
        RuleFor(x => x.CreditLimit).GreaterThan(0).When(x => x.CreditLimit.HasValue);
    }
}

public class UpdateCreditCardRequestValidator : AbstractValidator<UpdateCreditCardRequest>
{
    public UpdateCreditCardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Last4).Matches("^[0-9]{4}$").When(x => !string.IsNullOrEmpty(x.Last4))
            .WithMessage("Last4 must be exactly four digits.");
        RuleFor(x => x.CreditLimit).GreaterThan(0).When(x => x.CreditLimit.HasValue);
    }
}

public class CreateCardPaymentRequestValidator : AbstractValidator<CreateCardPaymentRequest>
{
    public CreateCardPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.PaymentDate).NotEqual(default(DateOnly)).WithMessage("Payment date is required.");
    }
}
