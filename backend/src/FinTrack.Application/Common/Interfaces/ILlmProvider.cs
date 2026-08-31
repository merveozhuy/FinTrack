namespace FinTrack.Application.Common.Interfaces;

public enum ChatRole
{
    User = 1,
    Assistant = 2,
}

public record ChatTurn(ChatRole Role, string Content);

/// <summary>
/// A grounded LLM request. The context is computed by backend services; the model's only job is to
/// explain it in natural language. History enables multi-turn conversations.
/// </summary>
public record LlmRequest(
    string SystemPrompt,
    string ContextText,
    IReadOnlyList<ChatTurn> History,
    string UserMessage);

public interface ILlmProvider
{
    Task<string> CompleteAsync(LlmRequest request, CancellationToken cancellationToken);
}
