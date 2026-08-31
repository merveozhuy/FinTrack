using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace FinTrack.Application.Features.Assistant.Documents;

public class FinancialDocumentService : IFinancialDocumentService
{
    private const int MonthsBack = 6;

    private readonly IAppDbContext _db;
    private readonly IEmbeddingProvider _embeddingProvider;

    public FinancialDocumentService(IAppDbContext db, IEmbeddingProvider embeddingProvider)
    {
        _db = db;
        _embeddingProvider = embeddingProvider;
    }

    public async Task EnsureDocumentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var changed = false;

        for (var i = 0; i < MonthsBack; i++)
        {
            var monthDate = today.AddMonths(-i);
            var range = MonthRange.For(monthDate.Year, monthDate.Month);
            var monthly = _db.Transactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.TransactionDate >= range.Start && t.TransactionDate <= range.End);

            var income = await monthly.Where(t => t.Type == TransactionType.Income)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
            var expense = await monthly.Where(t => t.Type == TransactionType.Expense)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

            if (income == 0m && expense == 0m)
            {
                continue;
            }

            var topCategories = await monthly.Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.Category!.Name)
                .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .Take(3)
                .ToListAsync(cancellationToken);

            var text = BuildSummaryText(monthDate.Year, monthDate.Month, income, expense,
                topCategories.Select(c => (c.Name, c.Amount)));
            var hash = Hash(text);

            var existing = await _db.EmbeddingDocuments.FirstOrDefaultAsync(
                d => d.UserId == userId && d.DocumentType == DocumentType.MonthlySummary && d.PeriodStart == range.Start,
                cancellationToken);

            if (existing is not null && existing.SourceHash == hash)
            {
                continue;
            }

            var embedding = await _embeddingProvider.EmbedAsync(text, cancellationToken);
            var vector = new Vector(new ReadOnlyMemory<float>(embedding));

            if (existing is not null)
            {
                existing.Content = text;
                existing.Embedding = vector;
                existing.SourceHash = hash;
                existing.PeriodEnd = range.End;
            }
            else
            {
                _db.EmbeddingDocuments.Add(new EmbeddingDocument
                {
                    UserId = userId,
                    DocumentType = DocumentType.MonthlySummary,
                    Content = text,
                    Embedding = vector,
                    PeriodStart = range.Start,
                    PeriodEnd = range.End,
                    SourceHash = hash,
                });
            }

            changed = true;
        }

        if (changed)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string BuildSummaryText(
        int year, int month, decimal income, decimal expense, IEnumerable<(string Name, decimal Amount)> topCategories)
    {
        var monthName = MonthRangeName(month);
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture,
            $"In {monthName} {year}, total income was {income} TRY and total expenses were {expense} TRY (net {income - expense} TRY).");

        var categories = topCategories.ToList();
        if (categories.Count > 0)
        {
            builder.Append(" Top spending categories: ");
            builder.Append(string.Join(", ", categories.Select(c =>
                string.Format(CultureInfo.InvariantCulture, "{0} {1} TRY", c.Name, c.Amount))));
            builder.Append('.');
        }

        return builder.ToString();
    }

    private static string MonthRangeName(int month) => new DateOnly(2000, month, 1)
        .ToString("MMMM", CultureInfo.InvariantCulture);

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
