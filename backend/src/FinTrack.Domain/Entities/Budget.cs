using FinTrack.Domain.Common;

namespace FinTrack.Domain.Entities;

public class Budget : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal MonthlyLimit { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
}
