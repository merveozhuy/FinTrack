using FinTrack.Application.Features.Assistant.Dtos;

namespace FinTrack.Application.Features.Assistant;

public interface IAssistantService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConversationSummaryDto>> GetConversationsAsync(CancellationToken cancellationToken);
    Task<ConversationDetailDto> GetConversationAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteConversationAsync(Guid id, CancellationToken cancellationToken);
}
