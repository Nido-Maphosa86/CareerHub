namespace CareerHub.Api.DTOs;

// Carries all client-supplied filter and sort parameters for the active listings query.
// Every property is optional — omitting a filter returns all results.
// Combining filters narrows with AND (not OR).
//
// Sort values:    postedAt (default) | salaryMin | salaryMax | title
// Dir values:     asc | desc  — overrides the default direction for any sort column
// Default directions when dir is omitted:
//   postedAt  → desc (newest first)
//   salaryMin → asc
//   salaryMax → desc
//   title     → asc

public record JobListingFilterQuery(
    // Case-insensitive partial match on Location
    string? Location       = null,

    // Exact match on Type enum name (e.g. "FullTime", "PartTime")
    string? EmploymentType = null,

    // Only return listings where SalaryMin >= this value
    decimal? SalaryMin     = null,

    // Only return listings where SalaryMax <= this value
    decimal? SalaryMax     = null,

    // Only return listings from this company
    Guid? CompanyId        = null,

    // Which column to sort on — defaults to postedAt
    string Sort            = "postedAt",

    // Override sort direction — null means use the default for the chosen sort column
    string? Dir            = null
);
