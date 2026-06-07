using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IJobListingRepository
{
    // ── Read ──────────────────────────────────────────────────────────────

    Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default);
    Task<JobDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken ct = default);
    Task<JobListing?> GetEntityByIdAsync(Guid id, CancellationToken ct = default);

    // Full-text search on Title and Description using the GIN index (Part 5).
    // Returns only Active, non-expired listings matching the search term.
    Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm, CancellationToken ct = default);

    // Application statistics per listing for a company (Part 8).
    // Uses RANK() window function — only expressible in raw SQL.
    Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(
        Guid companyId, CancellationToken ct = default);

    // ── Yes/No checks ─────────────────────────────────────────────────────

    Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────

    Task AddAsync(JobListing listing, CancellationToken ct = default);
    Task UpdateAsync(JobListing listing, CancellationToken ct = default);
    Task CloseAsync(Guid id, CancellationToken ct = default);
}
