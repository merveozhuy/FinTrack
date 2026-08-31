using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Assistant.Dtos;

public record ConversationSummaryDto(Guid Id, string Title, DateTime CreatedAt);

public record MessageDto(MessageRole Role, string Content, DateTime CreatedAt);

public record ConversationDetailDto(Guid Id, string Title, DateTime CreatedAt, IReadOnlyList<MessageDto> Messages);
