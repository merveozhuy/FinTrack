using FinTrack.Application.Features.Categories.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.Categories.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Type).IsInEnum();
    }
}
