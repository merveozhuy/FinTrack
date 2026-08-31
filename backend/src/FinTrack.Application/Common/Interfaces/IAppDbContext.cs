using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext exposed to the Application layer.
/// It intentionally exposes DbSets directly (no per-entity repository) to keep
/// query composition flexible while still allowing the context to be mocked in tests.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Budget> Budgets { get; }
    DbSet<RecurringTransaction> RecurringTransactions { get; }
    DbSet<AssistantConversation> Conversations { get; }
    DbSet<AssistantMessage> Messages { get; }
    DbSet<EmbeddingDocument> EmbeddingDocuments { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<CreditCard> CreditCards { get; }
    DbSet<CreditCardPayment> CreditCardPayments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
