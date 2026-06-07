namespace CareerHub.Api.Models;

// A listing moves from Active to Closed when the employer closes it
// or when the ClosingDate passes. Once Closed it cannot be updated or
// receive new applications. Stored as a string in the database.
public enum JobListingStatus
{
    Active,
    Closed
}
