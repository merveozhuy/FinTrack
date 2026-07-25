namespace FinTrack.Domain.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have access to this resource.") : base(message)
    {
    }
}
