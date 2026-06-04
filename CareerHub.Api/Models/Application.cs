namespace CareerHub.Api.Models;

// WHY IS THIS AN ENTITY AND NOT A HIDDEN JOIN TABLE?
//
// A hidden join table (automatically created by EF Core for many-to-many)
// only stores the two foreign keys. It carries no data of its own.
//
// An application is not just a link — it is a domain concept.
// It has a submission timestamp and a status that changes over time.
// A link table cannot store either of those. The moment you need to
// record WHEN something happened or WHAT STATE it is in, you need
// an explicit entity.
//
// Composite primary key: (JobListingId, ApplicantId)
// This is the naturally unique combination — one applicant can only
// apply once to a given listing. Configured in CareerHubDbContext.
//
// No data annotations — all configuration is in CareerHubDbContext Fluent API.

public class Application
{
    public Application() { }

    // ── Composite primary key (both columns together) ─────────────────────
    public Guid JobListingId { get; set; }
    public Guid ApplicantId { get; set; }

    // ── Application data ──────────────────────────────────────────────────
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    // ── Navigation properties ─────────────────────────────────────────────
    // null! suppresses the compiler nullable warning — EF Core
    // populates these when the relationship is loaded.
    public JobListing JobListing { get; set; } = null!;
    public Applicant Applicant { get; set; } = null!;
}
