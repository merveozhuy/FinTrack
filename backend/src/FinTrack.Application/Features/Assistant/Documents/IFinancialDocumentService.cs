namespace FinTrack.Application.Features.Assistant.Documents;

public interface IFinancialDocumentService
{
    /// <summary>
    /// Ensures the current user has up-to-date monthly summary documents (with embeddings) for the
    /// recent months that have activity. Re-embeds only when the underlying figures changed.
    /// </summary>
    Task EnsureDocumentsAsync(Guid userId, CancellationToken cancellationToken);
}
