namespace CareerHub.Api.Exceptions;

// Thrown when an operation is attempted on a listing whose Status is Closed.
// Service layer enforces: you cannot update or apply to a closed listing.
// GlobalExceptionHandler maps this to 409 Conflict.
public class ListingClosedException : Exception
{
    public ListingClosedException(Guid id)
        : base($"The job listing with ID {id} is closed and cannot be modified or applied to.") { }
}
