using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IJobListingRepository
{
    // ── Read ──────────────────────────────────────────────────────────────

    // Paginated, filtered, sorted — the main job board query
    Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(
        int page, int pageSize, JobListingFilterQuery filter, CancellationToken ct = default);

    // Employer's own listings — paginated
    Task<PagedResponse<JobResponse>> GetCompanyListingsPagedAsync(
        Guid companyId, int page, int pageSize, CancellationToken ct = default);

    Task<JobDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken ct = default);
    Task<JobListing?>        GetEntityByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(Guid companyId, CancellationToken ct = default);

    // ── Yes/No checks ─────────────────────────────────────────────────────

    Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────

    Task AddAsync(JobListing listing, CancellationToken ct = default);
    Task UpdateAsync(JobListing listing, CancellationToken ct = default);

    // Partial update — only non-null fields in the request are applied
    Task<JobResponse?> PatchAsync(Guid id, UpdateJobListingRequest request, CancellationToken ct = default);
    Task CloseAsync(Guid id, CancellationToken ct = default);
}
