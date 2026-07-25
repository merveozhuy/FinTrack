using FinTrack.Domain.Common;
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

public class EmbeddingDocumentConfiguration : IEntityTypeConfiguration<EmbeddingDocument>
{
    public void Configure(EntityTypeBuilder<EmbeddingDocument> builder)
    {
        builder.ToTable("embedding_documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.SourceHash).HasMaxLength(64);

        // Fixed-dimension pgvector column required for building an approximate index.
        builder.Property(x => x.Embedding)
            .HasColumnType($"vector({VectorConstants.EmbeddingDimensions})");

        // Every semantic query filters by UserId first, so index it.
        builder.HasIndex(x => x.UserId);

        // HNSW index with cosine distance for fast approximate nearest-neighbour search.
        builder.HasIndex(x => x.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.HasOne(x => x.User)
            .WithMany(u => u.EmbeddingDocuments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
