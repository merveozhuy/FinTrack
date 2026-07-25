namespace FinTrack.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the authenticated caller. Every data query filters by
/// <see cref="UserId"/> so that users can only ever see their own records.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
}
