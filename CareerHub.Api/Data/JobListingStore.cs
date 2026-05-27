using CareerHub.Api.Models;

namespace CareerHub.Api.Data;

// Static in-memory store — stands in for a database today only.
// The GUIDs are generated once when the class first loads.
// Week 2: this entire file disappears and is replaced by EF Core + PostgreSQL.
public static class JobListingStore
{
    public static readonly List<JobListing> Jobs =
    [
        new JobListing(
            Guid.NewGuid(),
            "Senior Backend Engineer",
            "Design and build scalable .NET microservices for the CareerHub talent platform.",
            "BitCube",
            "Bloemfontein, South Africa",
            JobType.FullTime,
            45_000,
            65_000,
            DateTime.UtcNow.AddDays(-10),
            true
        ),
        new JobListing(
            Guid.NewGuid(),
            "Frontend Developer",
            "Build delightful React and Next.js experiences for the CareerHub web app.",
            "Polar Studios",
            "Remote",
            JobType.Contract,
            null,
            null,
            DateTime.UtcNow.AddDays(-5),
            true
        ),
        new JobListing(
            Guid.NewGuid(),
            "DevOps Engineer",
            "Own the CI/CD pipeline and cloud infrastructure on Azure and AWS.",
            "Cloudwave",
            "Cape Town, South Africa",
            JobType.FullTime,
            50_000,
            null,
            DateTime.UtcNow.AddDays(-2),
            true
        ),
        new JobListing(
            Guid.NewGuid(),
            "Product Designer",
            "Lead UX research, wireframing and visual design for new CareerHub features.",
            "Pixel & Co",
            "Johannesburg, South Africa",
            JobType.PartTime,
            15_000,
            25_000,
            DateTime.UtcNow.AddDays(-1),
            true
        )
    ];
}
