using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class RecurringTransaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? Description { get; set; }

    public RecurrenceFrequency Frequency { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly NextExecutionDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Last date an occurrence was materialized. Used for idempotent generation.</summary>
    public DateOnly? LastExecutedDate { get; set; }

    public bool IsActive { get; set; } = true;
}
