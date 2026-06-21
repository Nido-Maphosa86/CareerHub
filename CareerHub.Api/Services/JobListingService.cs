using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

// ── Interface ────────────────────────────────────────────────────────────────

public interface IJobListingService
{
    Task<PagedResponse<JobResponse>>           GetActiveListingsAsync(int page, int pageSize, JobListingFilterQuery filter, CancellationToken ct = default);
    Task<PagedResponse<JobResponse>>           GetCompanyListingsAsync(Guid companyId, int page, int pageSize, CancellationToken ct = default);
    Task<JobDetailResponse>                    GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<JobResponse>>             SearchAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(Guid companyId, CancellationToken ct = default);
    Task<JobResponse>                          CreateAsync(CreateJobRequest request, CancellationToken ct = default);
    Task<JobResponse>                          UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct = default);
    Task<JobResponse>                          PatchAsync(Guid id, UpdateJobListingRequest request, CancellationToken ct = default);
    Task                                       CloseAsync(Guid id, CancellationToken ct = default);
}

// ── Implementation ───────────────────────────────────────────────────────────

public class JobListingService(
    IJobListingRepository listingRepo,
    ICompanyRepository    companyRepo) : IJobListingService
{
    public Task<PagedResponse<JobResponse>> GetActiveListingsAsync(
        int page, int pageSize, JobListingFilterQuery filter, CancellationToken ct = default) =>
        listingRepo.GetActiveListingsPagedAsync(page, pageSize, filter, ct);

    public Task<PagedResponse<JobResponse>> GetCompanyListingsAsync(
        Guid companyId, int page, int pageSize, CancellationToken ct = default) =>
        listingRepo.GetCompanyListingsPagedAsync(companyId, page, pageSize, ct);

    public async Task<JobDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await listingRepo.GetDetailByIdAsync(id, ct);
        if (listing is null) throw new JobNotFoundException(id);
        return listing;
    }

    public Task<IEnumerable<JobResponse>> SearchAsync(string searchTerm, CancellationToken ct = default) =>
        listingRepo.SearchAsync(searchTerm, ct);

    public Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(
        Guid companyId, CancellationToken ct = default) =>
        listingRepo.GetApplicationStatsAsync(companyId, ct);

    public async Task<JobResponse> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        // Validate company exists
        if (!await companyRepo.ExistsAsync(request.CompanyId!.Value, ct))
            throw new CompanyNotFoundException(request.CompanyId.Value);

        // Validate closing date is in the future
        if (request.ClosingDate!.Value <= DateTime.UtcNow)
            throw new InvalidListingException("Closing date must be in the future.");

        // Validate salary range — SalaryMax must be greater than SalaryMin
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue &&
            request.SalaryMax.Value <= request.SalaryMin.Value)
            throw new InvalidListingException("SalaryMax must be greater than SalaryMin.");

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
            throw new InvalidListingException("CompanyId does not match the listing's owning company.");

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

    public async Task<JobResponse> PatchAsync(Guid id, UpdateJobListingRequest request, CancellationToken ct = default)
    {
        // Check listing exists
        var existing = await listingRepo.GetEntityByIdAsync(id, ct);
        if (existing is null)
            throw new JobNotFoundException(id);

        // Check listing is not closed
        if (existing.Status == JobListingStatus.Closed)
            throw new ListingClosedException(id);

        // Validate salary range if either salary field is included in the PATCH
        if (request.SalaryMin is not null || request.SalaryMax is not null)
        {
            var effectiveMin = request.SalaryMin ?? existing.SalaryMin;
            var effectiveMax = request.SalaryMax ?? existing.SalaryMax;

            if (effectiveMin.HasValue && effectiveMax.HasValue &&
                effectiveMax.Value <= effectiveMin.Value)
                throw new InvalidListingException("SalaryMax must be greater than SalaryMin.");
        }

        // Validate closing date if included
        if (request.ClosingDate is not null && request.ClosingDate.Value <= DateTime.UtcNow)
            throw new InvalidListingException("Closing date must be in the future.");

        var result = await listingRepo.PatchAsync(id, request, ct);
        return result ?? throw new JobNotFoundException(id);
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
