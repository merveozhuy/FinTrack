using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class AssistantMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public AssistantConversation? Conversation { get; set; }

    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional JSON payload with the sources/data period used to build an answer.</summary>
    public string? MetadataJson { get; set; }
}
