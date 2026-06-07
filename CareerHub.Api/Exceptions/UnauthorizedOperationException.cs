namespace CareerHub.Api.Exceptions;

// Thrown when a user attempts an operation they do not own.
// Example: applicant1 trying to withdraw applicant2's application.
// GlobalExceptionHandler maps this to 403 Forbidden.
public class UnauthorizedOperationException : Exception
{
    public UnauthorizedOperationException(string message) : base(message) { }
}
