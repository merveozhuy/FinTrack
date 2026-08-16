using FinTrack.Application.Common.Interfaces;

namespace FinTrack.Application.Common;

public static class CurrentUserExtensions
{
    /// <summary>
    /// Returns the authenticated user's id or throws. Controllers guard endpoints with
    /// [Authorize], so this should always succeed there; the throw is a defensive guard
    /// for services called outside an authenticated request.
    /// </summary>
    public static Guid RequireUserId(this ICurrentUser currentUser) =>
        currentUser.UserId ?? throw new UnauthorizedAccessException("The request is not authenticated.");
}
