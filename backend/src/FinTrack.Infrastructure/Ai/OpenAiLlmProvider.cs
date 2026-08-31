using System.Net.Http;
using System.Net.Http.Json;
using FinTrack.Application.Common.Interfaces;

namespace FinTrack.Infrastructure.Ai;

/// <summary>OpenAI chat completions, used when configured with an API key. Off by default.</summary>
public class OpenAiLlmProvider : ILlmProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;

    public OpenAiLlmProvider(IHttpClientFactory httpClientFactory, AiOptions options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<string> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        var messages = new List<object>
        {
            new { role = "system", content = $"{request.SystemPrompt}\n\nContext (use only this):\n{request.ContextText}" },
        };
        foreach (var turn in request.History)
        {
            messages.Add(new { role = turn.Role == ChatRole.User ? "user" : "assistant", content = turn.Content });
        }
        messages.Add(new { role = "user", content = request.UserMessage });

        using var client = OpenAiEmbeddingProvider.CreateClient(_httpClientFactory, _options);
        var response = await client.PostAsJsonAsync(
            "chat/completions", new { model = _options.ChatModel, messages, temperature = 0.2 }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
        return payload!.Choices[0].Message.Content;
    }

    private sealed record ChatCompletionResponse(List<Choice> Choices);

    private sealed record Choice(Message Message);

    private sealed record Message(string Content);
}
