using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class ApplicationRepository(CareerHubDbContext db) : IApplicationRepository
{
    // ══════════════════════════════════════════════════════════════════════
    // COMPILED QUERY — Part 6 of Assignment 2.4
    //
    // HOT PATH: HasAlreadyAppliedAsync
    // Called on every application submission immediately after IsOpenForApplications.
    // Every logged-in applicant hitting POST /applications/{listingId} triggers this.
    // With 1,000 active daily users, this runs ~50–100 times per minute.
    //
    // The compiled query is a static readonly field — built once at startup,
    // shared across all requests. The public method signature is unchanged —
    // the service layer sees no difference.
    // ══════════════════════════════════════════════════════════════════════

    private static readonly Func<CareerHubDbContext, Guid, Guid, Task<bool>>
        _hasAlreadyApplied = EF.CompileAsyncQuery(
            (CareerHubDbContext ctx, Guid listingId, Guid applicantId) =>
                ctx.Applications.Any(a =>
                    a.JobListingId == listingId &&
                    a.ApplicantId  == applicantId));

    // ── Yes/No checks ─────────────────────────────────────────────────────

    // Delegates to the compiled query — the public interface is unchanged.
    public async Task<bool> HasAlreadyAppliedAsync(
        Guid listingId, Guid applicantId, CancellationToken ct = default) =>
        await _hasAlreadyApplied(db, listingId, applicantId);

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

    public async Task<Application?> GetEntityAsync(
        Guid listingId, Guid applicantId, CancellationToken ct = default) =>
        await db.Applications.FindAsync([applicantId, listingId], ct);

    // ── Write ─────────────────────────────────────────────────────────────

    public async Task AddAsync(Application application, CancellationToken ct = default)
    {
        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(
        Application application, ApplicationStatus newStatus, CancellationToken ct = default)
    {
        application.Status = newStatus;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Application application, CancellationToken ct = default)
    {
        db.Applications.Remove(application);
        await db.SaveChangesAsync(ct);
    }
}
