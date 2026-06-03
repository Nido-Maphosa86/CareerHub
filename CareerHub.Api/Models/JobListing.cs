namespace CareerHub.Api.Models;

// WHAT CHANGED FROM 2.1:
// - string Company removed — replaced with CompanyId (FK) and Company (navigation)
// - ICollection<Application> Applications added — the listings received applications
//
// No data annotations — all configuration is in CareerHubDbContext Fluent API.

public class JobListing
{
    public JobListing() { }

    public JobListing(
        Guid id,
        string title,
        string description,
        Guid companyId,
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
        CompanyId   = companyId;
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

    // ── Company relationship ───────────────────────────────────────────────
    // CompanyId is the foreign key column stored in the database.
    // Company is the navigation property — EF Core populates it when loaded.
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Location { get; set; } = string.Empty;

    public JobType Type { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // ── Applications received ─────────────────────────────────────────────
    public ICollection<Application> Applications { get; set; } = [];
}
