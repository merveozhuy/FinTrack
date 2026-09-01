using FinTrack.Application.Common.Interfaces;

namespace FinTrack.Infrastructure.Ai;

/// <summary>
/// Deterministic stand-in for a real LLM. It never invents figures: it presents the
/// backend-computed context (which already leads with a direct, intent-aware answer) in a readable
/// form. This keeps the app fully runnable offline while demonstrating the core principle — the
/// model explains numbers the backend already computed, it does not calculate them.
/// </summary>
public class FakeLlmProvider : ILlmProvider
{
    private const string Disclaimer = "Not: Bu yalnızca kişisel bütçe analizidir, yatırım tavsiyesi değildir.";

    public Task<string> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ContextText))
        {
            return Task.FromResult(
                "Bu konuda yeterli veriniz bulunmuyor. Birkaç işlem ekleyip tekrar sorabilirsiniz.");
        }

        return Task.FromResult($"{request.ContextText}\n\n{Disclaimer}");
    }
}
