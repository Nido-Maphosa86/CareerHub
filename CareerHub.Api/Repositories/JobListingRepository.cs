using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class JobListingRepository(CareerHubDbContext db) : IJobListingRepository
{
    // ══════════════════════════════════════════════════════════════════════
    // COMPILED QUERIES — Part 6 of Assignment 2.4
    //
    // A compiled query is a query plan that EF Core builds once at startup
    // and reuses on every subsequent call. Without compilation, EF Core
    // rebuilds the LINQ expression tree and translates it to SQL on every call.
    // For hot paths this overhead adds up across thousands of requests.
    //
    // HOT PATH 1: IsOpenForApplicationsAsync
    //   Called on every application submission — before allowing an applicant
    //   to apply. With 1,000 active daily users submitting applications,
    //   this runs ~50–100 times per minute. The compilation overhead is small
    //   per call but meaningful at this frequency.
    //
    // HOT PATH 2 is in ApplicationRepository (HasAlreadyAppliedAsync) —
    //   also called on every submission, immediately after this check.
    // ══════════════════════════════════════════════════════════════════════

    private static readonly Func<CareerHubDbContext, Guid, DateTime, Task<bool>>
        _isOpenForApplications = EF.CompileAsyncQuery(
            (CareerHubDbContext ctx, Guid id, DateTime now) =>
                ctx.JobListings.Any(j =>
                    j.Id == id &&
                    j.Status == JobListingStatus.Active &&
                    j.ClosingDate > now));

    // ── Read ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<JobResponse>> GetActiveListingsAsync(CancellationToken ct = default)
    {
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
            raw.ClosingDate, raw.Status.ToString()
        );
    }

    public async Task<JobListing?> GetEntityByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.FindAsync([id], ct);

    // ── FULL-TEXT SEARCH — Part 5 ─────────────────────────────────────────
    //
    // Uses the stored SearchVector computed column with the GIN index.
    // WebSearchToTsQuery converts plain user input ("senior developer") into
    // a tsquery safely — it handles special characters that would break ToTsQuery.
    // The GIN index makes this an Index Scan instead of a Seq Scan.
    // EXPLAIN ANALYZE confirms: "Bitmap Index Scan on ix_job_listings_searchvector".

    public async Task<IEnumerable<JobResponse>> SearchAsync(
        string searchTerm, CancellationToken ct = default)
    {
        var rawJobs = await db.JobListings
            .AsNoTracking()
            .Where(j =>
                j.Status == JobListingStatus.Active &&
                j.ClosingDate > DateTime.UtcNow &&
                j.SearchVector.Matches(
                    EF.Functions.WebSearchToTsQuery("english", searchTerm)))
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

    // ── RAW SQL — Part 8 ──────────────────────────────────────────────────
    //
    // WHY FromSql IS REQUIRED HERE:
    // This query uses two PostgreSQL features EF Core cannot translate:
    //
    // 1. RANK() OVER (ORDER BY COUNT(*) DESC)
    //    A window function that computes ranking across the result set.
    //    EF Core has no LINQ equivalent for window functions.
    //
    // 2. COUNT(*) FILTER (WHERE status = 'Submitted')
    //    Conditional aggregation — counts only rows matching a condition.
    //    EF Core cannot express this as a GroupBy/Select projection.
    //
    // PARAMETERISATION SAFETY:
    // String interpolation inside SqlQuery<T>($"...{companyId}...") is SAFE.
    // EF Core recognises interpolated expressions and converts them to
    // parameterised queries: @p0, @p1, etc. The value never becomes part
    // of the raw SQL string.
    //
    // UNSAFE: string.Format("...{0}...", companyId) or "WHERE id = " + companyId
    // These concatenate the value into the string BEFORE passing it to SqlQuery<T>.
    // EF Core receives a completed string with the value embedded — it cannot
    // extract parameters from a pre-built string. That is a SQL injection risk.

    public async Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(
        Guid companyId, CancellationToken ct = default)
    {
        // {companyId} is converted to @p0 by EF Core — never concatenated into the SQL string.
        return await db.Database.SqlQuery<JobListingStatsResponse>($"""
            SELECT
                j."Id"          AS "JobListingId",
                j."Title"       AS "Title",
                COUNT(a."JobListingId")    AS "TotalApplications",
                RANK() OVER (ORDER BY COUNT(a."JobListingId") DESC) AS "Rank",
                COUNT(*) FILTER (WHERE a."Status" = 'Submitted')    AS "SubmittedCount",
                COUNT(*) FILTER (WHERE a."Status" = 'UnderReview')  AS "UnderReviewCount",
                COUNT(*) FILTER (WHERE a."Status" = 'Shortlisted')  AS "ShortlistedCount",
                COUNT(*) FILTER (WHERE a."Status" = 'Rejected')     AS "RejectedCount",
                COUNT(*) FILTER (WHERE a."Status" = 'Offered')      AS "OfferedCount"
            FROM job_listings j
            LEFT JOIN applications a ON a."JobListingId" = j."Id"
            WHERE j."CompanyId" = {companyId}
              AND j."Status" = 'Active'
            GROUP BY j."Id", j."Title"
            ORDER BY "TotalApplications" DESC
            """)
            .ToListAsync(ct);
    }

    // ── Yes/No checks ─────────────────────────────────────────────────────

    // Delegates to compiled query — the public method signature is unchanged.
    public async Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default) =>
        await _isOpenForApplications(db, id, DateTime.UtcNow);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.AnyAsync(j => j.Id == id, ct);

    // ── Write ─────────────────────────────────────────────────────────────

    public async Task AddAsync(JobListing listing, CancellationToken ct = default)
    {
        db.JobListings.Add(listing);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobListing listing, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

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
