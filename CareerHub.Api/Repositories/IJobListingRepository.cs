using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

// The repository interface hides all EF Core details from the service layer.
// The service layer must be implementable without importing Microsoft.EntityFrameworkCore.
// Method names express INTENT — not generic CRUD operations.

public interface IJobListingRepository
{
    // ── Read methods ──────────────────────────────────────────────────────

    // Returns all Active listings with company name and application count.
    // The list endpoint — called on every page load of the job board.
    Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default);

    // Returns full detail for one listing including its applications.
    // Returns null if the listing does not exist.
    Task<JobDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken ct = default);

    // Returns the tracked domain entity for mutation (update / close).
    // Used by write operations — caller must not use AsNoTracking.
    Task<JobListing?> GetEntityByIdAsync(Guid id, CancellationToken ct = default);

    // ── Yes/No checks — named methods that return bool, not entity-or-null ──

    // True if the listing exists, is Active, and has not passed its ClosingDate.
    // Called by ApplicationService before allowing a new application.
    Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default);

    // True if a listing with this ID exists regardless of status.
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    // ── Write methods ─────────────────────────────────────────────────────

    // Persists a new listing. Caller should not call SaveChangesAsync.
    Task AddAsync(JobListing listing, CancellationToken ct = default);

    // Persists mutations to an existing tracked entity. Caller should not call SaveChangesAsync.
    Task UpdateAsync(JobListing listing, CancellationToken ct = default);

    // Marks a listing as Closed. No further updates or applications are accepted after this.
    Task CloseAsync(Guid id, CancellationToken ct = default);
}
