using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Services;

/// <summary>
/// Rule-based classifier that decides which data a question needs. It is intentionally NOT an LLM:
/// classification stays deterministic and free, and the LLM never decides what data to fetch.
/// </summary>
public static class QueryClassifier
{
    private static readonly string[] StructuredKeywords =
    {
        "how much", "total", "sum", "spent", "spend", "income", "expense", "balance", "net",
        "budget", "exceed", "over", "average", "percentage", "percent", "top", "most", "compare",
        "increase", "decrease", "upcoming", "ne kadar", "toplam", "harcad", "gelir", "gider",
        "bakiye", "bütçe", "aşt", "ortalama", "yüzde", "en çok", "karşılaştır", "artt", "azal",
        "kalan", "yaklaşan",
    };

    private static readonly string[] SemanticKeywords =
    {
        "like", "similar", "habit", "usually", "tend", "around", "describe", "summarize", "summary",
        "explain", "pattern", "benzer", "alışkanlık", "genelde", "eğilim", "civar", "açıkla",
        "yorumla", "özetle", "özet", "nasıl", "tasarruf", "dikkat",
    };

    public static QueryType Classify(string question)
    {
        var text = (question ?? string.Empty).ToLowerInvariant();

        var hasStructured = StructuredKeywords.Any(text.Contains);
        var hasSemantic = SemanticKeywords.Any(text.Contains);

        if (hasStructured && hasSemantic) return QueryType.Mixed;
        if (hasStructured) return QueryType.Structured;
        if (hasSemantic) return QueryType.Semantic;

        // No strong signal: gather both so the answer is well grounded.
        return QueryType.Mixed;
    }
}
