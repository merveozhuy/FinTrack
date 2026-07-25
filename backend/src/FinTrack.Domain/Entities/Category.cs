using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Category : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }

    /// <summary>True for categories seeded for every new user (e.g. Food, Salary).</summary>
    public bool IsDefault { get; set; }

    /// <summary>Soft-delete flag. Archived categories are hidden but keep historical references intact.</summary>
    public bool IsArchived { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
