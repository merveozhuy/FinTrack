using FinTrack.Domain.Enums;

namespace FinTrack.Application.Features.Assistant.Context;

public interface IAssistantContextBuilder
{
    Task<AssistantContext> BuildAsync(Guid userId, string question, QueryType queryType, CancellationToken cancellationToken);
}
