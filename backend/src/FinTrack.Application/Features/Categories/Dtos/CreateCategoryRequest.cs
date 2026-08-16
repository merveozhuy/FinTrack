using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Categories.Dtos;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
}
