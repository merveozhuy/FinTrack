using System.Security.Claims;
using FinTrack.Application.Common.Interfaces;

namespace FinTrack.Api.Services;

/// <summary>
/// Resolves the current user's id from the JWT claims on the incoming request.
/// Until authentication is wired up (Phase 3) this returns null, which is the
/// correct "anonymous" state.
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
