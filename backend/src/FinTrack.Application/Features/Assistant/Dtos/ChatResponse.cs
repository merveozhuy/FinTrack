namespace FinTrack.Application.Features.Assistant.Dtos;

public record DataPeriodDto(DateOnly Start, DateOnly End);

public record SourceRef(string Type, string? Category = null);

public record ChatResponse(
    string Answer,
    Guid ConversationId,
    DataPeriodDto DataPeriod,
    IReadOnlyList<SourceRef> Sources);
