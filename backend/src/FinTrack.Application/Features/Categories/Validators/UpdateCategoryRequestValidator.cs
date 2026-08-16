using FinTrack.Application.Features.Categories.Dtos;
using FluentValidation;

namespace FinTrack.Application.Features.Categories.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}
