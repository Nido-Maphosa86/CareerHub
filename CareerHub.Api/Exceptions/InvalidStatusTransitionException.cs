namespace CareerHub.Api.Exceptions;

// Thrown when an application status update violates the valid transition rules.
// Example: trying to move directly from Submitted to Offered skips UnderReview and Shortlisted.
// GlobalExceptionHandler maps this to 422 Unprocessable Entity.
public class InvalidStatusTransitionException : Exception
{
    public InvalidStatusTransitionException(string from, string to)
        : base($"Cannot transition an application from '{from}' to '{to}'. Check the valid workflow.") { }
}
