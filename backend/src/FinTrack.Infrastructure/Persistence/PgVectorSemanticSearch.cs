using FinTrack.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Persistence;

/// <summary>
/// Nearest-neighbour search over embedding documents using pgvector's cosine distance.
/// The query is always filtered by UserId, so a user can only ever retrieve their own documents.
/// </summary>
public class PgVectorSemanticSearch : ISemanticSearch
{
    private readonly AppDbContext _db;

    public PgVectorSemanticSearch(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DocumentMatch>> SearchAsync(
        Guid userId, float[] queryEmbedding, int topK, CancellationToken cancellationToken)
    {
        var query = new Vector(new ReadOnlyMemory<float>(queryEmbedding));

        return await _db.EmbeddingDocuments.AsNoTracking()
            .Where(d => d.UserId == userId && d.Embedding != null)
            .OrderBy(d => d.Embedding!.CosineDistance(query))
            .Take(topK)
            .Select(d => new DocumentMatch(d.Content, d.PeriodStart, d.PeriodEnd))
            .ToListAsync(cancellationToken);
    }
}
