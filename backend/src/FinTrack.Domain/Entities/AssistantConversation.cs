using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

public class AssistantConversation : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = string.Empty;

    public ICollection<AssistantMessage> Messages { get; set; } = new List<AssistantMessage>();
}
