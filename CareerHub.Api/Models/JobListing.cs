namespace CareerHub.Api.Models;

// Domain model — what the server stores internally.
// PostedAt and IsActive are server-owned: the client never supplies them.
// SalaryMin / SalaryMax are stored as raw numbers;
// the human-readable SalaryDisplay is computed in the response DTO, not stored here.
public record JobListing(
    Guid id,
    string Title,
    string Description,
    string Company,
    string Location,
    JobType Type,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTime PostedAt,  // set by the server at the moment of creation
    bool IsActive       // set by the server — defaults to true on creation
);

