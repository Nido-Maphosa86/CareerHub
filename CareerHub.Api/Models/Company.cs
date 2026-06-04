namespace CareerHub.Api.Models;

// Company is an independent entity — it exists regardless of whether it has
// any job listings. Storing company as a plain string (as in 2.1) means:
// - no referential integrity ("BitCube" and "Bitcube" become different companies)
// - company-level data (website, industry) would be duplicated on every listing
// - querying all jobs for a company requires string matching
// A proper entity solves all three.
//
// No data annotations — all configuration is in CareerHubDbContext Fluent API.

public class Company
{
    // Parameterless constructor required by EF Core for materialising entities
    public Company() { }

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string? Industry { get; set; }

    // Collection navigation — EF Core populates this when Include() or a projection is used.
    // Initialised to an empty list so it is never null before EF Core loads it.
    public ICollection<JobListing> JobListings { get; set; } = [];
}
