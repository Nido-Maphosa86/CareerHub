namespace CareerHub.Api.Models;

// WHY A CLASS AND NOT A RECORD?
// EF Core's change tracker works by loading an entity, taking a snapshot
// of its state, and detecting changes when SaveChangesAsync() is called.
// Records are immutable (init-only properties) — the change tracker
// cannot mutate them to reflect what came back from the database.
// A plain class with public setters is what EF Core expects.
//
// NOTE: No data annotations ([Key], [Required], [MaxLength]) here.
// All database configuration lives in CareerHubDbContext using the Fluent API.
// This keeps the entity clean and decoupled from the persistence layer.

public class JobListing
{
    // Parameterless constructor — EF Core uses this when loading
    // entities from the database (materialising results from a query).
    public JobListing() { }

    // Convenience constructor — used in the controller when creating
    // a brand-new listing before saving to the database.
    public JobListing(
        Guid id,
        string title,
        string description,
        string company,
        string location,
        JobType type,
        decimal? salaryMin,
        decimal? salaryMax,
        DateTime postedAt,
        bool isActive)
    {
        Id          = id;
        Title       = title;
        Description = description;
        Company     = company;
        Location    = location;
        Type        = type;
        SalaryMin   = salaryMin;
        SalaryMax   = salaryMax;
        PostedAt    = postedAt;
        IsActive    = isActive;
    }

    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    // Stored as a string in the database ("FullTime" not 0)
    // configured in OnModelCreating via .HasConversion<string>()
    public JobType Type { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    // Server sets this at creation time — never supplied by the client
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    // Server sets this — defaults to true on creation
    public bool IsActive { get; set; } = true;
}
