using FinTrack.Application.Features.Transactions.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.Transactions.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.TransactionDate).NotEqual(default(DateOnly)).WithMessage("Transaction date is required.");
        RuleFor(x => x.Description).MaximumLength(512);
    }
}
