namespace FinTrack.Application.Common.Interfaces;

public record DocumentMatch(string Content, DateOnly PeriodStart, DateOnly PeriodEnd);

/// <summary>
/// Nearest-neighbour search over a user's embedding documents. Implemented in Infrastructure with
/// pgvector. The <paramref name="userId"/> filter is mandatory so one user can never retrieve
/// another user's documents.
/// </summary>
public interface ISemanticSearch
{
    Task<IReadOnlyList<DocumentMatch>> SearchAsync(
        Guid userId, float[] queryEmbedding, int topK, CancellationToken cancellationToken);
}
