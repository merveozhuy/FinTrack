namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Turns text into an embedding vector. Provider-agnostic so the app can run offline with a
/// deterministic fake provider and switch to OpenAI (or another provider) via configuration.
/// </summary>
public interface IEmbeddingProvider
{
    int Dimensions { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);
}
