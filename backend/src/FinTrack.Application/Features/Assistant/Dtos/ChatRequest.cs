namespace FinTrack.Application.Features.Assistant.Dtos;

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}
