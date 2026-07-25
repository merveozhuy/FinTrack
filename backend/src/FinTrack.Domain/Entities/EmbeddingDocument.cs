using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using Pgvector;

namespace FinTrack.Domain.Entities;

public class EmbeddingDocument : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DocumentType DocumentType { get; set; }

    /// <summary>Human-readable text that was embedded (also used as retrieval context).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>pgvector embedding. Nullable so a document row can exist before embedding is generated.</summary>
    public Vector? Embedding { get; set; }

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Deterministic hash of the source data, used to avoid regenerating unchanged documents.</summary>
    public string SourceHash { get; set; } = string.Empty;
}
