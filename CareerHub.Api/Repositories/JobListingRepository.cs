using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

// EF Core is ONLY imported here — not in any service or controller.
// All projections, AsNoTracking, Include, and ToListAsync calls live in this class.

public class JobListingRepository(CareerHubDbContext db) : IJobListingRepository
{
    // ── Read ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default)
    {
        // Projection: only the columns JobResponse needs.
        // ApplicationCount is computed by the database — COUNT(*) in SQL.
        // Company.Name is a JOIN — only the Name column, not the whole Company entity.
        // AsNoTracking: read-only — no snapshot overhead.
        var rawJobs = await db.JobListings
            .AsNoTracking()
            .Where(j => j.Status == JobListingStatus.Active && j.ClosingDate > DateTime.UtcNow)
            .OrderByDescending(j => j.PostedAt)
            .Select(j => new
            {
                j.Id, j.Title, j.Description,
                CompanyName      = j.Company.Name,
                j.Location, j.Type, j.SalaryMin, j.SalaryMax,
                j.PostedAt, j.IsActive, j.ClosingDate, j.Status,
                ApplicationCount = j.Applications.Count()
            })
            .ToListAsync(ct);

        return rawJobs.Select(j => new JobResponse(
            j.Id, j.Title, j.Description, j.CompanyName,
            j.Location, j.Type, j.SalaryMin, j.SalaryMax,
            ComputeSalaryDisplay(j.SalaryMin, j.SalaryMax),
            j.PostedAt, j.IsActive, j.ApplicationCount,
            j.ClosingDate, j.Status.ToString()
        ));
    }

    public async Task<JobDetailResponse?> GetDetailByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Projection: fetch only what JobDetailResponse needs.
        // Nested Select on Applications gets applicant name and status — not their email.
        var raw = await db.JobListings
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new
            {
                j.Id, j.Title, j.Description,
                CompanyName  = j.Company.Name,
                j.Location, j.Type, j.SalaryMin, j.SalaryMax,
                j.PostedAt, j.IsActive, j.ClosingDate, j.Status,
                Applications = j.Applications.Select(a => new
                {
                    ApplicantName = a.Applicant.Name,
                    a.SubmittedAt,
                    a.Status
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (raw is null) return null;

        return new JobDetailResponse(
            raw.Id, raw.Title, raw.Description, raw.CompanyName,
            raw.Location, raw.Type, raw.SalaryMin, raw.SalaryMax,
            ComputeSalaryDisplay(raw.SalaryMin, raw.SalaryMax),
            raw.PostedAt, raw.IsActive,
            raw.Applications.Select(a => new ApplicationSummary(
                a.ApplicantName, a.SubmittedAt, a.Status.ToString()
            )),
            raw.ClosingDate,
            raw.Status.ToString()
        );
    }

    // Returns a tracked entity for mutation — no AsNoTracking
    public async Task<JobListing?> GetEntityByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.FindAsync([id], ct);

    // ── Yes/No checks ─────────────────────────────────────────────────────

    public async Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.AnyAsync(j =>
            j.Id == id &&
            j.Status == JobListingStatus.Active &&
            j.ClosingDate > DateTime.UtcNow, ct);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.AnyAsync(j => j.Id == id, ct);

    // ── Write ─────────────────────────────────────────────────────────────

    public async Task AddAsync(JobListing listing, CancellationToken ct = default)
    {
        // Add() registers the entity in the change tracker as Added.
        // SaveChangesAsync generates the INSERT statement.
        db.JobListings.Add(listing);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobListing listing, CancellationToken ct = default)
    {
        // The entity was loaded via GetEntityByIdAsync — it is already tracked.
        // The service mutated its properties. SaveChangesAsync generates the UPDATE.
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await db.JobListings.FindAsync([id], ct);
        if (listing is null) return;

        listing.Status   = JobListingStatus.Closed;
        listing.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static string ComputeSalaryDisplay(decimal? min, decimal? max) =>
        (min, max) switch
        {
            (not null, not null) => $"R{min.Value:N0} – R{max.Value:N0}/month",
            (not null, null)     => $"From R{min.Value:N0}/month",
            _                    => "Salary not specified"
        };
}
