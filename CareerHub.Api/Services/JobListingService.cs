using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

// ── Interface ────────────────────────────────────────────────────────────────

public interface IJobListingService
{
    Task<IEnumerable<JobResponse>>          GetActiveListingsAsync(CancellationToken ct = default);
    Task<JobDetailResponse>                 GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<JobResponse>>          SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(Guid companyId, CancellationToken ct = default);
    Task<JobResponse>                       CreateAsync(CreateJobRequest request, CancellationToken ct = default);
    Task<JobResponse>                       UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct = default);
    Task                                    CloseAsync(Guid id, CancellationToken ct = default);
}

// ── Implementation ───────────────────────────────────────────────────────────

// No Microsoft.EntityFrameworkCore imports. If EF Core types appear here,
// that code belongs in the repository instead.

public class JobListingService(
    IJobListingRepository listingRepo,
    ICompanyRepository    companyRepo) : IJobListingService
{
    public Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default) =>
        listingRepo.GetActiveListingsAsync(ct);

    public async Task<JobDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await listingRepo.GetDetailByIdAsync(id, ct);
        if (listing is null) throw new JobNotFoundException(id);
        return listing;
    }

    // Delegates directly — the service owns no search logic.
    // The repository decides how to query (FTS via GIN index).
    public Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm, CancellationToken ct = default) =>
        listingRepo.SearchAsync(searchTerm, ct);

    // Delegates directly — the repository owns the raw SQL query.
    public Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(
        Guid companyId, CancellationToken ct = default) =>
        listingRepo.GetApplicationStatsAsync(companyId, ct);

    public async Task<JobResponse> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        if (!await companyRepo.ExistsAsync(request.CompanyId!.Value, ct))
            throw new CompanyNotFoundException(request.CompanyId.Value);

        if (request.ClosingDate!.Value <= DateTime.UtcNow)
            throw new InvalidListingException("Closing date must be in the future.");

        var listing = new JobListing(
            Guid.NewGuid(), request.Title, request.Description,
            request.CompanyId.Value, request.Location, request.Type!.Value,
            request.SalaryMin, request.SalaryMax,
            DateTime.UtcNow, true, request.ClosingDate.Value, JobListingStatus.Active
        );

        await listingRepo.AddAsync(listing, ct);
        var detail = await listingRepo.GetDetailByIdAsync(listing.Id, ct);
        return ToSummary(detail!);
    }

    public async Task<JobResponse> UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct = default)
    {
        var existing = await listingRepo.GetEntityByIdAsync(id, ct);
        if (existing is null) throw new JobNotFoundException(id);

        if (existing.Status == JobListingStatus.Closed)
            throw new ListingClosedException(id);

        if (existing.CompanyId != request.CompanyId!.Value)
            throw new InvalidListingException(
                "The CompanyId in the request does not match the listing's owning company.");

        if (!await companyRepo.ExistsAsync(request.CompanyId.Value, ct))
            throw new CompanyNotFoundException(request.CompanyId.Value);

        existing.Title       = request.Title;
        existing.Description = request.Description;
        existing.Location    = request.Location;
        existing.Type        = request.Type!.Value;
        existing.SalaryMin   = request.SalaryMin;
        existing.SalaryMax   = request.SalaryMax;
        existing.ClosingDate = request.ClosingDate!.Value;

        await listingRepo.UpdateAsync(existing, ct);
        var detail = await listingRepo.GetDetailByIdAsync(id, ct);
        return ToSummary(detail!);
    }

    public async Task CloseAsync(Guid id, CancellationToken ct = default)
    {
        if (!await listingRepo.ExistsAsync(id, ct))
            throw new JobNotFoundException(id);
        await listingRepo.CloseAsync(id, ct);
    }

    private static JobResponse ToSummary(JobDetailResponse d) =>
        new(d.id, d.Title, d.Description, d.CompanyName, d.Location, d.Type,
            d.SalaryMin, d.SalaryMax, d.SalaryDisplay, d.PostedAt, d.IsActive,
            d.Applications.Count(), d.ClosingDate, d.Status);
}
