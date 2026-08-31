using FinTrack.Application.Features.Budgets.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.Budgets.Validators;

public class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).WithMessage("Monthly limit must be greater than zero.");
    }
}
