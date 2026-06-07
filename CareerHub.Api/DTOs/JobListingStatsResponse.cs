namespace CareerHub.Api.DTOs;

// Response DTO for GET /jobs/stats?companyId={id}
// Populated by a raw SQL query using RANK() and conditional aggregation —
// two PostgreSQL features that EF Core cannot translate from LINQ.
// Property names must match the SQL column aliases exactly (case-insensitive).

public record JobListingStatsResponse(
    Guid   JobListingId,
    string Title,
    long   TotalApplications,   // COUNT of all applications
    long   Rank,                // RANK() by TotalApplications — rank 1 = most applied to
    long   SubmittedCount,      // COUNT(*) FILTER (WHERE status = 'Submitted')
    long   UnderReviewCount,
    long   ShortlistedCount,
    long   RejectedCount,
    long   OfferedCount
);
