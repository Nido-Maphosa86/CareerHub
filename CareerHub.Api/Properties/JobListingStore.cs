using CareerHub.Api.Models;

namespace CareerHub.Api;

public static class JobListingStore
{
    public static readonly List<JobListing> jobs = new()
    {
        new JobListing(
            Guid.NewGuid(),
            "Senior Backend Engineer",
            "Design and build scalable .NET microservices for the CareerHub talent platform.",
            "BitCube",
            "Bloemfontein, South Africa",
            "Full-Time"
        ),
        new JobListing(
            Guid.NewGuid(),
            "Frontend Developer",
            "Build delightful React and Next.js experiences for the CareerHub web app.",
            "Polar Studios",
            "Remote",
            "Contract"
        ),
        new JobListing(
            Guid.NewGuid(),
            "DevOps Engineer",
            "Own the CI/CD pipeline and cloud infrastructure on Azure and AWS.",
            "Cloudwave",
            "Cape Town, South Africa",
            "Full-Time"
        ),
        new JobListing(
            Guid.NewGuid(),
            "Product Designer",
            "Lead UX research, wireframing and visual design for new CareerHub features.",
            "Pixel & Co",
            "Johannesburg, South Africa",
            "Part-Time"
        )
    };
}
