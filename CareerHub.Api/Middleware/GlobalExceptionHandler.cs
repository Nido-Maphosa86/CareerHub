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
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        var statusCode = exception switch
        {
            JobNotFoundException          => StatusCodes.Status404NotFound,
            CompanyNotFoundException      => StatusCodes.Status404NotFound,
            DuplicateJobListingException  => StatusCodes.Status409Conflict,
            DuplicateApplicationException => StatusCodes.Status409Conflict,
            ListingClosedException        => StatusCodes.Status409Conflict,
            InvalidListingException       => StatusCodes.Status400BadRequest,
            InvalidStatusTransitionException => StatusCodes.Status422UnprocessableEntity,
            UnauthorizedOperationException   => StatusCodes.Status403Forbidden,
            _                             => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = GetTitle(statusCode),
            Detail   = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest          => "Bad Request",
        StatusCodes.Status403Forbidden           => "Forbidden",
        StatusCodes.Status404NotFound            => "Resource Not Found",
        StatusCodes.Status409Conflict            => "Resource Conflict",
        StatusCodes.Status422UnprocessableEntity => "Invalid Status Transition",
        _                                        => "Internal Server Error"
    };
}
