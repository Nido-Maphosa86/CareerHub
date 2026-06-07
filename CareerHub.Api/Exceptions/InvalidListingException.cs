namespace CareerHub.Api.Exceptions;

// Thrown when a listing request violates a domain rule —
// e.g. closing date is in the past, or update targets a company the listing does not belong to.
// GlobalExceptionHandler maps this to 400 Bad Request.
public class InvalidListingException : Exception
{
    public InvalidListingException(string message) : base(message) { }
}
