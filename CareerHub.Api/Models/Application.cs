namespace CareerHub.Api.Models;

// WHY IS THIS AN ENTITY AND NOT A HIDDEN JOIN TABLE?
//
// A hidden join table (automatically created by EF Core for many-to-many)
// only stores the two foreign keys. It carries no data of its own.
//
// An application is not just a link — it is a domain concept.
// It has a submission timestamp, a status that changes over time, and now
// the details the applicant submitted when they applied. A link table cannot
// store any of that.
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

    // ── Application metadata ──────────────────────────────────────────────
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    // ── Submitted application details (Assignment 1.4 — frontend form) ────
    // Captured as a snapshot at apply time. Kept on the application itself so
    // the record stays accurate even if the applicant later edits their profile.
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int YearsOfExperience { get; set; }
    public string CoverLetter { get; set; } = string.Empty;
    public string? LinkedInUrl { get; set; }
    public bool AvailableImmediately { get; set; }
    public int NoticePeriodWeeks { get; set; }

    // ── Navigation properties ─────────────────────────────────────────────
    // null! suppresses the compiler nullable warning — EF Core
    // populates these when the relationship is loaded.
    public JobListing JobListing { get; set; } = null!;
    public Applicant Applicant { get; set; } = null!;
}
