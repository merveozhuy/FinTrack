using FinTrack.Application.Features.Assistant.Dtos;

namespace FinTrack.Application.Features.Assistant.Context;

/// <summary>The grounded context handed to the LLM, plus the metadata returned to the client.</summary>
public record AssistantContext(
    string ContextText,
    DataPeriodDto Period,
    IReadOnlyList<SourceRef> Sources,
    bool HasData);
