namespace FinTrack.Domain.Exceptions;

/// <summary>
/// Base type for expected, business-level exceptions. These are translated to
/// specific HTTP status codes by the global exception middleware.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}
