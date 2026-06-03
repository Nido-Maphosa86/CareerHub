using CareerHub.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CareerHub.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the error
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        // 2. Map exception type to HTTP status code
        var statusCode = exception switch
        {
            JobNotFoundException         => StatusCodes.Status404NotFound,
            CompanyNotFoundException     => StatusCodes.Status404NotFound,
            DuplicateJobListingException => StatusCodes.Status409Conflict,
            DuplicateApplicationException => StatusCodes.Status409Conflict,
            _                            => StatusCodes.Status500InternalServerError
        };

        // 3. Construct Problem Details response
        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = GetTitle(statusCode),
            Detail   = exception.Message,
            Instance = httpContext.Request.Path
        };

        // 4. Write the response
        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status404NotFound  => "Resource Not Found",
        StatusCodes.Status409Conflict  => "Resource Conflict",
        _                              => "Internal Server Error"
    };
}
