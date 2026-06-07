using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public interface IApplicationRepository
{
    // ── Yes/No checks — named methods that return bool ────────────────────

    // True if this applicant has already submitted an application for this listing.
    // Called on every application submission — it is a hot path.
    Task<bool> HasAlreadyAppliedAsync(Guid listingId, Guid applicantId, CancellationToken ct = default);

    // ── Read ──────────────────────────────────────────────────────────────

    // All applications for a listing — used by the employer dashboard.
    Task<IEnumerable<ApplicationResponse>> GetByListingIdAsync(Guid listingId, CancellationToken ct = default);

    // All applications submitted by a specific applicant — their history view.
    Task<IEnumerable<ApplicationResponse>> GetByApplicantIdAsync(Guid applicantId, CancellationToken ct = default);

    // Returns the tracked entity for mutation (status update or withdrawal).
    Task<Application?> GetEntityAsync(Guid listingId, Guid applicantId, CancellationToken ct = default);

    // ── Write ─────────────────────────────────────────────────────────────

    Task AddAsync(Application application, CancellationToken ct = default);
    Task UpdateStatusAsync(Application application, ApplicationStatus newStatus, CancellationToken ct = default);
    Task DeleteAsync(Application application, CancellationToken ct = default);
}

public class ApplicationRepository(CareerHubDbContext db) : IApplicationRepository
{
    // ── Yes/No checks ─────────────────────────────────────────────────────

    public async Task<bool> HasAlreadyAppliedAsync(Guid listingId, Guid applicantId, CancellationToken ct = default) =>
        await db.Applications.AnyAsync(a =>
            a.JobListingId == listingId &&
            a.ApplicantId  == applicantId, ct);

    // ── Read ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ApplicationResponse>> GetByListingIdAsync(
        Guid listingId, CancellationToken ct = default) =>
        await db.Applications
            .AsNoTracking()
            .Where(a => a.JobListingId == listingId)
            .Select(a => new ApplicationResponse(
                a.JobListingId,
                a.JobListing.Title,
                a.JobListing.Company.Name,
                a.ApplicantId,
                a.Applicant.Name,
                a.SubmittedAt,
                a.Status.ToString()))
            .ToListAsync(ct);

    public async Task<IEnumerable<ApplicationResponse>> GetByApplicantIdAsync(
        Guid applicantId, CancellationToken ct = default) =>
        await db.Applications
            .AsNoTracking()
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationResponse(
                a.JobListingId,
                a.JobListing.Title,
                a.JobListing.Company.Name,
                a.ApplicantId,
                a.Applicant.Name,
                a.SubmittedAt,
                a.Status.ToString()))
            .ToListAsync(ct);

    // Composite PK: (ApplicantId, JobListingId) — order matches HasKey in DbContext
    public async Task<Application?> GetEntityAsync(Guid listingId, Guid applicantId, CancellationToken ct = default) =>
        await db.Applications.FindAsync([applicantId, listingId], ct);

    // ── Write ─────────────────────────────────────────────────────────────

    public async Task AddAsync(Application application, CancellationToken ct = default)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Application application, ApplicationStatus newStatus, CancellationToken ct = default)
    {
        // The entity is already tracked — mutate the property and save.
        // The change tracker generates: UPDATE applications SET status = @newStatus WHERE ...
        application.Status = newStatus;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Application application, CancellationToken ct = default)
    {
        db.Applications.Remove(application);
        await db.SaveChangesAsync(ct);
    }
}
