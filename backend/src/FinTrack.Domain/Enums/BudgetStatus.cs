namespace FinTrack.Domain.Enums;

public enum BudgetStatus
{
    /// <summary>Spending is under the warning threshold.</summary>
    Ok = 1,

    /// <summary>Spending has reached the warning threshold (default 80%) but not exceeded the limit.</summary>
    Warning = 2,

    /// <summary>Spending is above the monthly limit.</summary>
    Exceeded = 3
}
