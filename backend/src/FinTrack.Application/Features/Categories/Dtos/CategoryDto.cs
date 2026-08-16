using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Categories.Dtos;

public record CategoryDto(Guid Id, string Name, CategoryType Type, bool IsDefault, bool IsArchived);
