using FinTrack.Application.Common.Interfaces;

namespace FinTrack.Infrastructure.Ai;

/// <summary>
/// Deterministic stand-in for a real LLM. It never invents figures: it simply presents the
/// backend-computed context in a readable form (and the standard disclaimer). This keeps the app
/// fully runnable offline while demonstrating the core principle — the model explains numbers the
/// backend already computed, it does not calculate them.
/// </summary>
public class FakeLlmProvider : ILlmProvider
{
    private const string Disclaimer = "This is personal budget analysis only, not investment advice.";

    public Task<string> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContextText) ||
            request.ContextText.Contains("no recorded transactions", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                "I don't have enough data about your finances yet. Add some transactions and ask me again.");
        }

        var answer = $"Here is what your data shows:\n\n{request.ContextText}\n\n{Disclaimer}";
        return Task.FromResult(answer);
    }
}
