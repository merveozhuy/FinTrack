using FluentValidation;
using ValidationException = FinTrack.Domain.Exceptions.ValidationException;

namespace FinTrack.Application.Common.Validation;

public static class ValidatorExtensions
{
    /// <summary>
    /// Runs the validator and, on failure, throws the domain <see cref="ValidationException"/>
    /// (field -> messages) which the API middleware renders as a ProblemDetails 400.
    /// </summary>
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

        throw new ValidationException(errors);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
