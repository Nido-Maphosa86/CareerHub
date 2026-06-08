using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// PATCH DTO — every field is nullable.
// Null means "leave this field unchanged".
// Only non-null fields are applied to the listing.
//
// WHY nullable DTO instead of JSON Patch (RFC 6902)?
//   Nullable DTO resolves the PUT race condition: two recruiters each send only
//   the field they changed, so they cannot overwrite each other's work.
//   Limitation: you cannot set a field back to null using this approach — null
//   means "don't change". JSON Patch handles this with an explicit "remove"
//   operation. For CareerHub, salary fields can never be unset once set, so
//   this limitation does not affect the current requirements.

public record UpdateJobListingRequest(
    string?      Title          = null,
    string?      Description    = null,
    string?      Location       = null,
    JobType?     EmploymentType = null,
    decimal?     SalaryMin      = null,
    decimal?     SalaryMax      = null,
    DateTime?    ClosingDate    = null
);
