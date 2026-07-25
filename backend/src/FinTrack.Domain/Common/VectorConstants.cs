namespace FinTrack.Domain.Common;

public static class VectorConstants
{
    /// <summary>
    /// Embedding dimension used across the app. 1536 matches OpenAI text-embedding-3-small,
    /// which is the first planned provider; the Fake provider produces vectors of this size too
    /// so the schema stays provider-agnostic.
    /// </summary>
    public const int EmbeddingDimensions = 1536;
}
