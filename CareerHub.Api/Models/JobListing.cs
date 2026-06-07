namespace CareerHub.Api.Models;

// WHAT CHANGED FROM 2.2:
// - ClosingDate added — service enforces it must be in the future at creation time
// - Status added — Active by default, moves to Closed when employer closes the listing
//   or when ClosingDate passes
//
// No data annotations — all configuration is in CareerHubDbContext Fluent API.

public class JobListing
{
    // Parameterless constructor — EF Core uses this when loading from the database
    public JobListing() { }

    // Convenience constructor — used in JobListingService when creating a new listing
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
        bool isActive,
        DateTime closingDate,
        JobListingStatus status = JobListingStatus.Active)
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
        ClosingDate = closingDate;
        Status      = status;
    }

    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Foreign key and navigation to Company entity
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Location { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    // Server-owned — set at creation time, never supplied by the client
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // When this listing stops accepting applications — must be in the future at creation time
    public DateTime ClosingDate { get; set; }

    // Active = accepting applications | Closed = no longer accepting
    public JobListingStatus Status { get; set; } = JobListingStatus.Active;

    // All applications this listing has received
    public ICollection<Application> Applications { get; set; } = [];
}
