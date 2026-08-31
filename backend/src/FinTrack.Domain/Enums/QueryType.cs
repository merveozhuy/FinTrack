namespace FinTrack.Domain.Enums;

public enum QueryType
{
    /// <summary>Needs exact figures computed by backend services (totals, budgets, balances).</summary>
    Structured = 1,

    /// <summary>Needs semantic retrieval over the user's own summary documents.</summary>
    Semantic = 2,

    /// <summary>Needs both exact figures and semantic context.</summary>
    Mixed = 3,
}
