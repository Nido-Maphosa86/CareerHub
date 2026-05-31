using CareerHub.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CareerHub.Api.Middleware;

// IExceptionHandler is the interface for typed exception handling.
// AddExceptionHandler<T>() registers it; UseExceptionHandler() activates it.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the error before doing anything else
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Translate the domain exception to an HTTP status code
        var statusCode = exception switch
        {
            JobNotFoundException         => StatusCodes.Status404NotFound,
            DuplicateJobListingException => StatusCodes.Status409Conflict,
            _                            => StatusCodes.Status500InternalServerError
        };

        // 3. Construct the Problem Details response shape
        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = GetTitle(statusCode),
            Detail   = exception.Message,
            Instance = httpContext.Request.Path
        };

        // 4. Write the status code and JSON body back to the client
        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // true = exception has been handled, stop propagation
    }

    // Maps an HTTP status code to a short human-readable title
    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound  => "Resource Not Found",
        StatusCodes.Status409Conflict  => "Resource Conflict",
        _                              => "Internal Server Error"
    };
}
