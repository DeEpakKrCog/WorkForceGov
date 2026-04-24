using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkForceGovProject.Exceptions;

namespace WorkForceGovProject.Middleware;

/// <summary>
/// Global exception handler — catches every unhandled exception, logs it via
/// Serilog, and returns a structured RFC-7807 ProblemDetails response.
///
/// Registration (Program.cs):
///   builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;();
///   builder.Services.AddProblemDetails();
///   ...
///   app.UseExceptionHandler();
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        // ── Log every exception with full details ────────────────────────────
        _logger.LogError(
            exception,
            "An error has occurred while processing the request. TraceId {TraceId}",
            traceId);

        // ── Map exception type → status code + title ────────────────────────
        int statusCode;
        string title;

        switch (exception)
        {
            case NotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title      = "Resource Not Found";
                break;

            case ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                title      = "Validation Error";
                break;

            case UnauthorizedException:
                statusCode = StatusCodes.Status403Forbidden;
                title      = "Forbidden";
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                title      = "Unauthorized";
                break;

            case ArgumentNullException:
            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                title      = "Bad Request";
                break;

            case InvalidOperationException:
                statusCode = StatusCodes.Status409Conflict;
                title      = "Conflict";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title      = "Internal Server Error";
                break;
        }

        // ── Build ProblemDetails response ────────────────────────────────────
        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        // Include validation errors dictionary if present
        if (exception is ValidationException valEx && valEx.Errors.Count > 0)
            problem.Extensions["errors"] = valEx.Errors;

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // exception is handled — pipeline stops here
    }
}
