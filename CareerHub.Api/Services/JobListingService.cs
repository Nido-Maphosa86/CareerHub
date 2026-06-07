using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

// ── Interface ────────────────────────────────────────────────────────────────

public interface IJobListingService
{
    Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default);
    Task<JobDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<JobResponse> CreateAsync(CreateJobRequest request, CancellationToken ct = default);
    Task<JobResponse> UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct = default);
    Task CloseAsync(Guid id, CancellationToken ct = default);
}

// ── Implementation ───────────────────────────────────────────────────────────

// IMPORTANT: No using directive for Microsoft.EntityFrameworkCore in this file.
// If EF Core types (AnyAsync, ToListAsync, Include, AsNoTracking) appear here,
// that code belongs in the repository instead.

public class JobListingService(
    IJobListingRepository listingRepo,
    ICompanyRepository    companyRepo) : IJobListingService
{
    // ── Read — delegate directly to the repository ────────────────────────

    public Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default) =>
        listingRepo.GetActiveListingsAsync(ct);

    public async Task<JobDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await listingRepo.GetDetailByIdAsync(id, ct);

        // Business rule: throw if not found — controller never checks for null
        if (listing is null)
            throw new JobNotFoundException(id);

        return listing;
    }

    // ── Write — business rules live here, EF Core calls live in the repository ──

    public async Task<JobResponse> CreateAsync(CreateJobRequest request, CancellationToken ct = default)
    {
        // Rule 1: Company must exist before a listing can be created for it.
        // Wrong choice: putting this check in the controller makes the controller
        // responsible for a business rule — it would need to know which repository
        // to call and what the consequence of a missing company is.
        if (!await companyRepo.ExistsAsync(request.CompanyId!.Value, ct))
            throw new CompanyNotFoundException(request.CompanyId.Value);

        // Rule 2: The closing date must be in the future.
        // If we skip this check, employers could create listings that are
        // already closed at the moment of creation — no one could ever apply.
        if (request.ClosingDate!.Value <= DateTime.UtcNow)
            throw new InvalidListingException("Closing date must be in the future.");

        var listing = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.CompanyId.Value,
            request.Location,
            request.Type!.Value,
            request.SalaryMin,
            request.SalaryMax,
            DateTime.UtcNow,
            true,
            request.ClosingDate.Value,
            JobListingStatus.Active
        );

        await listingRepo.AddAsync(listing, ct);

        // Re-fetch as a projection so the response includes CompanyName
        var detail = await listingRepo.GetDetailByIdAsync(listing.Id, ct);
        return ToSummary(detail!);
    }

    public async Task<JobResponse> UpdateAsync(Guid id, UpdateJobRequest request, CancellationToken ct = default)
    {
        // Load the tracked entity so the change tracker can detect mutations
        var existing = await listingRepo.GetEntityByIdAsync(id, ct);
        if (existing is null)
            throw new JobNotFoundException(id);

        // Rule 3: A closed listing cannot be updated.
        // Allowing updates on closed listings would let employers reopen them
        // silently without going through the proper channels.
        if (existing.Status == JobListingStatus.Closed)
            throw new ListingClosedException(id);

        // Rule 4: The CompanyId in the update must match the listing's CompanyId.
        // This prevents one company from claiming ownership of another's listing.
        if (existing.CompanyId != request.CompanyId!.Value)
            throw new InvalidListingException(
                "The CompanyId in the request does not match the listing's owning company.");

        // Verify the new company still exists (edge case: company was deleted)
        if (!await companyRepo.ExistsAsync(request.CompanyId.Value, ct))
            throw new CompanyNotFoundException(request.CompanyId.Value);

        // Mutate the tracked entity — repository generates the UPDATE statement
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

    // ── Private helpers ───────────────────────────────────────────────────

    // Converts a JobDetailResponse to the lighter JobResponse used by list/write endpoints.
    private static JobResponse ToSummary(JobDetailResponse detail) =>
        new(detail.id, detail.Title, detail.Description, detail.CompanyName,
            detail.Location, detail.Type, detail.SalaryMin, detail.SalaryMax,
            detail.SalaryDisplay, detail.PostedAt, detail.IsActive,
            detail.Applications.Count(), detail.ClosingDate, detail.Status);
}
