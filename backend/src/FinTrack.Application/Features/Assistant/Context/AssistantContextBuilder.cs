using System.Globalization;
using System.Text;
using FinTrack.Application.Common;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Application.Features.Assistant.Documents;
using FinTrack.Application.Features.Assistant.Dtos;
using FinTrack.Application.Features.Dashboard;
using FinTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Application.Features.Assistant.Context;

public class AssistantContextBuilder : IAssistantContextBuilder
{
    private const int TopK = 3;

    private readonly IAppDbContext _db;
    private readonly IDashboardService _dashboard;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ISemanticSearch _semanticSearch;
    private readonly IFinancialDocumentService _documents;

    public AssistantContextBuilder(
        IAppDbContext db,
        IDashboardService dashboard,
        IEmbeddingProvider embeddingProvider,
        ISemanticSearch semanticSearch,
        IFinancialDocumentService documents)
    {
        _db = db;
        _dashboard = dashboard;
        _embeddingProvider = embeddingProvider;
        _semanticSearch = semanticSearch;
        _documents = documents;
    }

    public async Task<AssistantContext> BuildAsync(Guid userId, string question, QueryType queryType, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var range = MonthRange.For(today.Year, today.Month);
        var period = new DataPeriodDto(range.Start, range.End);
        var sources = new List<SourceRef>();

        var hasTransactions = await _db.Transactions.AnyAsync(t => t.UserId == userId, cancellationToken);
        if (!hasTransactions)
        {
            return new AssistantContext("The user has no recorded transactions yet.", period, sources, false);
        }

        var builder = new StringBuilder();

        // Structured, backend-computed facts (the LLM never calculates these).
        var dashboard = await _dashboard.GetAsync(today.Year, today.Month, cancellationToken);
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Period: {period.Start:yyyy-MM-dd} to {period.End:yyyy-MM-dd}.");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Total income: {dashboard.TotalIncome} TRY. Total expense: {dashboard.TotalExpense} TRY. Net balance: {dashboard.NetBalance} TRY.");
        if (dashboard.ExpenseChangePercent.HasValue)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"Expense change vs previous month: {dashboard.ExpenseChangePercent}%.");
        }
        sources.Add(new SourceRef("MonthlySummary"));

        if (dashboard.TopExpenseCategories.Count > 0)
        {
            builder.AppendLine("Top expense categories:");
            foreach (var category in dashboard.TopExpenseCategories)
            {
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"- {category.CategoryName}: {category.Amount} TRY ({category.Percentage}%).");
            }
        }

        var alerts = dashboard.Budgets
            .Where(b => b.Status == BudgetStatus.Exceeded || b.Status == BudgetStatus.Warning)
            .ToList();
        if (alerts.Count > 0)
        {
            builder.AppendLine("Budget alerts:");
            foreach (var alert in alerts)
            {
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"- {alert.CategoryName}: spent {alert.Spent} of {alert.Limit} TRY ({alert.UsagePercentage}%, {alert.Status}).");
                sources.Add(new SourceRef("BudgetSummary", alert.CategoryName));
            }
        }

        if (dashboard.UpcomingPayments.Count > 0)
        {
            builder.AppendLine("Upcoming payments:");
            foreach (var payment in dashboard.UpcomingPayments)
            {
                builder.AppendLine(CultureInfo.InvariantCulture,
                    $"- {payment.CategoryName}: {payment.Amount} TRY on {payment.NextExecutionDate:yyyy-MM-dd}.");
            }
        }

        // Semantic retrieval over the user's own monthly summary documents.
        if (queryType is QueryType.Semantic or QueryType.Mixed)
        {
            await _documents.EnsureDocumentsAsync(userId, cancellationToken);
            var queryEmbedding = await _embeddingProvider.EmbedAsync(question, cancellationToken);
            var matches = await _semanticSearch.SearchAsync(userId, queryEmbedding, TopK, cancellationToken);
            if (matches.Count > 0)
            {
                builder.AppendLine("Relevant history:");
                foreach (var match in matches)
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"- {match.Content}");
                }
                sources.Add(new SourceRef("EmbeddingDocument"));
            }
        }

        return new AssistantContext(builder.ToString().TrimEnd(), period, sources, true);
    }
}
