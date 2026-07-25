namespace FinTrack.Domain.Exceptions;

/// <summary>
/// Raised when one or more input fields fail validation. Carries a field -> messages map
/// that the middleware surfaces in the ProblemDetails "errors" extension.
/// </summary>
public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
