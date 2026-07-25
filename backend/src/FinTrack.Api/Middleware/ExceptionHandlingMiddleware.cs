using System.Text.Json;
using FinTrack.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails responses.
/// Expected business exceptions map to specific status codes; anything else becomes a 500
/// with a generic message so internal details are never leaked to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private const string ErrorTypeBase = "https://fintrack.dev/errors/";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, slug) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation error", "validation"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "not-found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", "conflict"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden", "forbidden"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", "unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "internal")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning("Handled {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = ErrorTypeBase + slug,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }
}
