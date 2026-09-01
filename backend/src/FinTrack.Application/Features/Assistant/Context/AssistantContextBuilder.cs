using System.Globalization;
using System.Text;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Features.Assistant.Documents;
using FinTrack.Application.Features.Assistant.Dtos;
using FinTrack.Domain.Enums;
using FinTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Features.Assistant.Context;

public class AssistantContextBuilder : IAssistantContextBuilder
{
    private const int TopK = 3;
    private const int UpcomingHorizonDays = 30;

    private static readonly string[] TrMonths =
    {
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık",
    };

    private readonly IAppDbContext _db;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ISemanticSearch _semanticSearch;
    private readonly IFinancialDocumentService _documents;

    public AssistantContextBuilder(
        IAppDbContext db,
        IEmbeddingProvider embeddingProvider,
        ISemanticSearch semanticSearch,
        IFinancialDocumentService documents)
    {
        _db = db;
        _embeddingProvider = embeddingProvider;
        _semanticSearch = semanticSearch;
        _documents = documents;
    }

    public async Task<AssistantContext> BuildAsync(Guid userId, string question, QueryType queryType, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var period = PeriodResolver.Resolve(question, today);
        var periodDto = new DataPeriodDto(period.Start, period.End);
        var sources = new List<SourceRef>();

        var hasTransactions = await _db.Transactions.AnyAsync(t => t.UserId == userId, cancellationToken);
        if (!hasTransactions)
        {
            return new AssistantContext(string.Empty, periodDto, sources, false);
        }

        // Backend-computed facts for the requested period (the LLM never calculates these).
        var income = await SumAsync(userId, TransactionType.Income, period.Start, period.End, cancellationToken);
        var expense = await SumAsync(userId, TransactionType.Expense, period.Start, period.End, cancellationToken);

        var prevStart = period.Start.AddMonths(-period.MonthsSpan);
        var prevEnd = period.Start.AddDays(-1);
        var prevExpense = await SumAsync(userId, TransactionType.Expense, prevStart, prevEnd, cancellationToken);
        decimal? expenseChange = prevExpense > 0
            ? Math.Round((expense - prevExpense) / prevExpense * 100m, 1)
            : null;

        var categories = await ExpenseByCategoryAsync(userId, period.Start, period.End, expense, cancellationToken);
        var cards = await CardDebtsAsync(userId, cancellationToken);
        var upcoming = await UpcomingAsync(userId, today, cancellationToken);
        var alerts = await BudgetAlertsAsync(userId, today, cancellationToken);

        sources.Add(new SourceRef("MonthlySummary"));
        if (cards.Count > 0) sources.Add(new SourceRef("CreditCard"));
        foreach (var alert in alerts) sources.Add(new SourceRef("BudgetSummary", alert.Category));

        var builder = new StringBuilder();

        // A direct, intent-aware answer sentence at the top, so even the fake LLM "answers".
        builder.AppendLine(BuildDirectAnswer(question, period, income, expense, expenseChange, categories, cards, upcoming, alerts));
        builder.AppendLine();

        builder.AppendLine($"Dönem: {period.Label} ({period.Start:yyyy-MM-dd} – {period.End:yyyy-MM-dd}).");
        builder.AppendLine($"Toplam gelir: {Money(income)}. Toplam gider: {Money(expense)}. Net bakiye: {Money(income - expense)}.");
        if (expenseChange.HasValue)
        {
            var direction = expenseChange.Value >= 0 ? "arttı" : "azaldı";
            builder.AppendLine($"Önceki döneme göre gider %{Pct(Math.Abs(expenseChange.Value))} {direction}.");
        }
        if (income > 0)
        {
            builder.AppendLine($"Tasarruf oranı: %{Pct(Math.Round((income - expense) / income * 100m, 1))}.");
        }

        if (categories.Count > 0)
        {
            builder.AppendLine("En çok harcanan kategoriler:");
            foreach (var c in categories.Take(5))
            {
                builder.AppendLine($"- {c.Name}: {Money(c.Amount)} (%{Pct(c.Percentage)}).");
            }
        }

        if (period.MonthsSpan > 1)
        {
            var trend = await MonthlyTrendAsync(userId, period, cancellationToken);
            if (trend.Count > 0)
            {
                builder.AppendLine("Aylık gider trendi:");
                foreach (var point in trend)
                {
                    builder.AppendLine($"- {point.Label}: {Money(point.Amount)}.");
                }
            }
        }

        if (alerts.Count > 0)
        {
            builder.AppendLine("Bütçe uyarıları (bu ay):");
            foreach (var a in alerts)
            {
                builder.AppendLine($"- {a.Category}: {Money(a.Spent)} / {Money(a.Limit)} (%{Pct(a.Usage)}, {a.Status}).");
            }
        }

        if (cards.Count > 0)
        {
            builder.AppendLine("Kredi kartı borçları:");
            foreach (var card in cards)
            {
                builder.AppendLine($"- {card.Name}: {Money(card.Debt)}.");
            }
        }

        if (upcoming.Count > 0)
        {
            builder.AppendLine("Yaklaşan ödemeler (30 gün):");
            foreach (var u in upcoming)
            {
                builder.AppendLine($"- {u.Category}: {Money(u.Amount)}, {u.Date:yyyy-MM-dd}.");
            }
        }

        // Semantic context from the user's own monthly-summary documents.
        if (queryType is QueryType.Semantic or QueryType.Mixed)
        {
            await _documents.EnsureDocumentsAsync(userId, cancellationToken);
            var queryEmbedding = await _embeddingProvider.EmbedAsync(question, cancellationToken);
            var matches = await _semanticSearch.SearchAsync(userId, queryEmbedding, TopK, cancellationToken);
            if (matches.Count > 0)
            {
                builder.AppendLine("İlgili geçmiş özetler:");
                foreach (var match in matches)
                {
                    builder.AppendLine($"- {match.Content}");
                }
                sources.Add(new SourceRef("EmbeddingDocument"));
            }
        }

        return new AssistantContext(builder.ToString().TrimEnd(), periodDto, sources, true);
    }

    private static string BuildDirectAnswer(
        string question,
        ResolvedPeriod period,
        decimal income,
        decimal expense,
        decimal? expenseChange,
        IReadOnlyList<(string Name, decimal Amount, decimal Percentage)> categories,
        IReadOnlyList<(string Name, decimal Debt)> cards,
        IReadOnlyList<(string Category, decimal Amount, DateOnly Date)> upcoming,
        IReadOnlyList<(string Category, decimal Spent, decimal Limit, decimal Usage, string Status)> alerts)
    {
        var q = question.ToLowerInvariant();

        if (Has(q, "kart", "borç", "borc", "card", "debt"))
        {
            if (cards.Count == 0) return "Özet cevap: Tanımlı bir kredi kartınız bulunmuyor.";
            var total = cards.Sum(c => c.Debt);
            return $"Özet cevap: Toplam kredi kartı borcunuz {Money(total)}.";
        }

        if (Has(q, "yaklaşan", "yaklasan", "fatura", "upcoming", "due", "bill"))
        {
            if (upcoming.Count == 0) return "Özet cevap: Önümüzdeki 30 günde yaklaşan ödemeniz yok.";
            return $"Özet cevap: Önümüzdeki 30 günde {upcoming.Count} ödeme, toplam {Money(upcoming.Sum(u => u.Amount))}.";
        }

        if (Has(q, "bütçe", "butce", "aşt", "ast", "aşım", "asim", "budget", "exceed"))
        {
            return alerts.Count == 0
                ? "Özet cevap: Bu ay bütçe aşımınız yok."
                : $"Özet cevap: {alerts.Count} kategoride bütçe uyarısı/aşımı var.";
        }

        if (Has(q, "tasarruf", "biriktir", "saving", "save"))
        {
            if (income <= 0) return "Özet cevap: Bu dönemde gelir kaydınız olmadığı için tasarruf oranı hesaplanamıyor.";
            var rate = Math.Round((income - expense) / income * 100m, 1);
            return $"Özet cevap: {period.Label} tasarruf oranınız %{Pct(rate)} (net {Money(income - expense)}).";
        }

        if (Has(q, "arttı", "artti", "azaldı", "azaldi", "göre", "gore", "değiş", "degis", "karşılaştır", "karsilastir", "compare", "change", "increase", "decrease"))
        {
            if (!expenseChange.HasValue) return $"Özet cevap: {period.Label} gideriniz {Money(expense)}; karşılaştıracak önceki dönem verisi yok.";
            var direction = expenseChange.Value >= 0 ? "arttı" : "azaldı";
            return $"Özet cevap: {period.Label} gideriniz {Money(expense)}; önceki döneme göre %{Pct(Math.Abs(expenseChange.Value))} {direction}.";
        }

        if (Has(q, "en çok", "en cok", "en fazla", "hangi kategori", "most", "top", "en yüksek", "en yuksek"))
        {
            return categories.Count == 0
                ? $"Özet cevap: {period.Label} gider kaydınız yok."
                : $"Özet cevap: {period.Label} en çok harcama {categories[0].Name} kategorisinde: {Money(categories[0].Amount)}.";
        }

        return $"Özet cevap: {period.Label} toplam gideriniz {Money(expense)}, geliriniz {Money(income)} (net {Money(income - expense)}).";
    }

    private async Task<decimal> SumAsync(Guid userId, TransactionType type, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == type && t.TransactionDate >= start && t.TransactionDate <= end)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
    }

    private async Task<List<(string Name, decimal Amount, decimal Percentage)>> ExpenseByCategoryAsync(
        Guid userId, DateOnly start, DateOnly end, decimal totalExpense, CancellationToken cancellationToken)
    {
        var groups = await _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.TransactionDate >= start && t.TransactionDate <= end)
            .GroupBy(t => t.Category!.Name)
            .Select(g => new { Name = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return groups
            .OrderByDescending(g => g.Amount)
            .Select(g => (g.Name, g.Amount, totalExpense > 0 ? Math.Round(g.Amount / totalExpense * 100m, 1) : 0m))
            .ToList();
    }

    private async Task<List<(string Label, decimal Amount)>> MonthlyTrendAsync(Guid userId, ResolvedPeriod period, CancellationToken cancellationToken)
    {
        var result = new List<(string, decimal)>();
        var cursor = new DateOnly(period.Start.Year, period.Start.Month, 1);
        while (cursor <= period.End)
        {
            var monthEnd = cursor.AddMonths(1).AddDays(-1);
            var effectiveEnd = monthEnd > period.End ? period.End : monthEnd;
            var amount = await SumAsync(userId, TransactionType.Expense, cursor, effectiveEnd, cancellationToken);
            result.Add(($"{TrMonths[cursor.Month - 1]} {cursor.Year}", amount));
            cursor = cursor.AddMonths(1);
        }
        return result;
    }

    private async Task<List<(string Name, decimal Debt)>> CardDebtsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cards = await _db.CreditCards.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);
        if (cards.Count == 0) return new List<(string, decimal)>();

        var spent = (await _db.Transactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.CreditCardId != null)
                .GroupBy(t => t.CreditCardId!.Value)
                .Select(g => new { CardId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.CardId, x => x.Amount);
        var paid = (await _db.CreditCardPayments.AsNoTracking()
                .Where(p => p.UserId == userId)
                .GroupBy(p => p.CreditCardId)
                .Select(g => new { CardId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.CardId, x => x.Amount);

        return cards
            .Select(c => (c.Name, spent.GetValueOrDefault(c.Id, 0m) - paid.GetValueOrDefault(c.Id, 0m)))
            .Where(c => c.Item2 != 0m)
            .ToList();
    }

    private async Task<List<(string Category, decimal Amount, DateOnly Date)>> UpcomingAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var horizon = today.AddDays(UpcomingHorizonDays);
        var rows = await _db.RecurringTransactions.AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive && r.NextExecutionDate >= today && r.NextExecutionDate <= horizon)
            .OrderBy(r => r.NextExecutionDate)
            .Select(r => new { Category = r.Category!.Name, r.Amount, r.NextExecutionDate })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.Category, r.Amount, r.NextExecutionDate)).ToList();
    }

    private async Task<List<(string Category, decimal Spent, decimal Limit, decimal Usage, string Status)>> BudgetAlertsAsync(
        Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var budgets = await _db.Budgets.AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == today.Year && b.Month == today.Month)
            .Select(b => new { b.CategoryId, CategoryName = b.Category!.Name, b.MonthlyLimit })
            .ToListAsync(cancellationToken);
        if (budgets.Count == 0) return new List<(string, decimal, decimal, decimal, string)>();

        var spentByCategory = (await _db.Transactions.AsNoTracking()
                .Where(t => t.UserId == userId && t.Type == TransactionType.Expense && t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
                .GroupBy(t => t.CategoryId)
                .Select(g => new { CategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.CategoryId, x => x.Amount);

        var alerts = new List<(string, decimal, decimal, decimal, string)>();
        foreach (var b in budgets)
        {
            var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0m);
            var calc = BudgetCalculator.Calculate(b.MonthlyLimit, spent);
            if (calc.Status is BudgetStatus.Warning or BudgetStatus.Exceeded)
            {
                var status = calc.Status == BudgetStatus.Exceeded ? "aşıldı" : "uyarı";
                alerts.Add((b.CategoryName, calc.Spent, calc.Limit, calc.UsagePercentage, status));
            }
        }
        return alerts;
    }

    private static bool Has(string text, params string[] terms) => terms.Any(text.Contains);

    private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture) + " TL";

    private static string Pct(decimal value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}
