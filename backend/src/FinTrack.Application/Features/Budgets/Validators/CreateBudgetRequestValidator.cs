using FinTrack.Application.Features.Budgets.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.Budgets.Validators;

public class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).WithMessage("Monthly limit must be greater than zero.");
    }
}
