using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Common;

namespace FinTrack.Infrastructure.Ai;

/// <summary>
/// Deterministic, dependency-free embedding provider used by default so the app (and CI) run
/// without any API key. It hashes tokens into a fixed-size bag-of-words vector: texts that share
/// words land closer together, which is enough to exercise the semantic-search pipeline offline.
/// A real provider (OpenAI) is swapped in via configuration.
/// </summary>
public class FakeEmbeddingProvider : IEmbeddingProvider
{
    public int Dimensions => VectorConstants.EmbeddingDimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        var vector = new float[Dimensions];

        foreach (var token in Tokenize(text))
        {
            var index = (int)(Fnv1A(token) % (uint)Dimensions);
            vector[index] += 1f;
        }

        Normalize(vector);
        return Task.FromResult(vector);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return (text ?? string.Empty)
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => new string(t.Where(char.IsLetterOrDigit).ToArray()))
            .Where(t => t.Length > 0);
    }

    // Stable across process runs (unlike string.GetHashCode) so stored and query embeddings align.
    private static uint Fnv1A(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }
        return hash;
    }

    private static void Normalize(float[] vector)
    {
        double sumSquares = 0;
        foreach (var value in vector)
        {
            sumSquares += value * value;
        }

        if (sumSquares <= 0)
        {
            return;
        }

        var norm = (float)Math.Sqrt(sumSquares);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }
}
