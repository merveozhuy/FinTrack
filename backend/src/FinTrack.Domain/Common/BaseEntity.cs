namespace FinTrack.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Uses a client-generated GUID key so that
/// related graphs can be built before the entity is saved.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Entity that also tracks its last modification time. <see cref="UpdatedAt"/> is
/// maintained centrally in the DbContext SaveChanges override.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
