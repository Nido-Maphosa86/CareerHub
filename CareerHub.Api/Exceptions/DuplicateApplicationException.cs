namespace CareerHub.Api.Exceptions;

public class DuplicateApplicationException : Exception
{
    public DuplicateApplicationException(Guid jobId)
        : base($"You have already applied for this job listing.") { }
}
