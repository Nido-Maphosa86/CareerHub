using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Repositories;

public class JobListingRepository(CareerHubDbContext db) : IJobListingRepository
{
    // ══════════════════════════════════════════════════════════════════════
    // COMPILED QUERIES — hot paths called on every application submission
    // ══════════════════════════════════════════════════════════════════════

    private static readonly Func<CareerHubDbContext, Guid, DateTime, Task<bool>>
        _isOpenForApplications = EF.CompileAsyncQuery(
            (CareerHubDbContext ctx, Guid id, DateTime now) =>
                ctx.JobListings.Any(j =>
                    j.Id == id &&
                    j.Status == JobListingStatus.Active &&
                    j.ClosingDate > now));

    // ── PAGINATED ACTIVE LISTINGS — Part 3 + Part 4 ──────────────────────

    public async Task<PagedResponse<JobResponse>> GetActiveListingsPagedAsync(
        int page, int pageSize, JobListingFilterQuery filter, CancellationToken ct = default)
    {
        // Start with the base active listings query
        IQueryable<JobListing> query = db.JobListings
            .AsNoTracking()
            .Where(j => j.Status == JobListingStatus.Active && j.ClosingDate > DateTime.UtcNow);

        // ── Apply filters — each Where is only added when the parameter is non-null ──
        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(j => j.Location.ToLower().Contains(filter.Location.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.EmploymentType))
            query = query.Where(j => j.Type.ToString() == filter.EmploymentType);

        if (filter.SalaryMin.HasValue)
            query = query.Where(j => j.SalaryMin >= filter.SalaryMin.Value);

        if (filter.SalaryMax.HasValue)
            query = query.Where(j => j.SalaryMax <= filter.SalaryMax.Value);

        if (filter.CompanyId.HasValue)
            query = query.Where(j => j.CompanyId == filter.CompanyId.Value);

        // ── Apply sorting — OrderBy MUST appear before Skip for deterministic pagination ──
        // Default direction when dir is omitted:
        //   postedAt → desc | salaryMin → asc | salaryMax → desc | title → asc
        query = (filter.Sort.ToLower(), filter.Dir?.ToLower()) switch
        {
            ("postedat",  "asc")  => query.OrderBy(j => j.PostedAt),
            ("postedat",  _)      => query.OrderByDescending(j => j.PostedAt),  // default desc
            ("salarymin", "desc") => query.OrderByDescending(j => j.SalaryMin),
            ("salarymin", _)      => query.OrderBy(j => j.SalaryMin),           // default asc
            ("salarymax", "asc")  => query.OrderBy(j => j.SalaryMax),
            ("salarymax", _)      => query.OrderByDescending(j => j.SalaryMax), // default desc
            ("title",     "desc") => query.OrderByDescending(j => j.Title),
            ("title",     _)      => query.OrderBy(j => j.Title),               // default asc
            _                     => query.OrderByDescending(j => j.PostedAt)   // fallback
        };

        // ── Two queries — same IQueryable ensures count and data are always consistent ──
        var totalCount = await query.CountAsync(ct);

        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id, j.Title, j.Description,
                CompanyName      = j.Company.Name,
                j.Location, j.Type, j.SalaryMin, j.SalaryMax,
                j.PostedAt, j.IsActive, j.ClosingDate, j.Status,
                ApplicationCount = j.Applications.Count()
            })
            .ToListAsync(ct);

        var items = rawItems.Select(j => new JobResponse(
            j.Id, j.Title, j.Description, j.CompanyName,
            j.Location, j.Type, j.SalaryMin, j.SalaryMax,
            ComputeSalaryDisplay(j.SalaryMin, j.SalaryMax),
            j.PostedAt, j.IsActive, j.ApplicationCount,
            j.ClosingDate, j.Status.ToString()
        ));

        return PagedResponse<JobResponse>.Create(items, page, pageSize, totalCount);
    }

    // ── EMPLOYER'S OWN LISTINGS — paginated ──────────────────────────────

    public async Task<PagedResponse<JobResponse>> GetCompanyListingsPagedAsync(
        Guid companyId, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<JobListing> query = db.JobListings
            .AsNoTracking()
            .Where(j => j.CompanyId == companyId)
            .OrderByDescending(j => j.PostedAt);

        var totalCount = await query.CountAsync(ct);

        var rawItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id, j.Title, j.Description,
                CompanyName      = j.Company.Name,
                j.Location, j.Type, j.SalaryMin, j.SalaryMax,
                j.PostedAt, j.IsActive, j.ClosingDate, j.Status,
                ApplicationCount = j.Applications.Count()
            })
            .ToListAsync(ct);

        var items = rawItems.Select(j => new JobResponse(
            j.Id, j.Title, j.Description, j.CompanyName,
            j.Location, j.Type, j.SalaryMin, j.SalaryMax,
            ComputeSalaryDisplay(j.SalaryMin, j.SalaryMax),
            j.PostedAt, j.IsActive, j.ApplicationCount,
            j.ClosingDate, j.Status.ToString()
        ));

        return PagedResponse<JobResponse>.Create(items, page, pageSize, totalCount);
    }

    // ── DETAIL READ ───────────────────────────────────────────────────────

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

    // ── FULL-TEXT SEARCH (Part 5 of 2.4) ─────────────────────────────────

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

    // ── RAW SQL STATS (Part 8 of 2.4) ────────────────────────────────────

    public async Task<IEnumerable<JobListingStatsResponse>> GetApplicationStatsAsync(
        Guid companyId, CancellationToken ct = default)
    {
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

    // ── YES/NO CHECKS ────────────────────────────────────────────────────

    public async Task<bool> IsOpenForApplicationsAsync(Guid id, CancellationToken ct = default) =>
        await _isOpenForApplications(db, id, DateTime.UtcNow);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        await db.JobListings.AnyAsync(j => j.Id == id, ct);

    // ── WRITE ─────────────────────────────────────────────────────────────

    public async Task AddAsync(JobListing listing, CancellationToken ct = default)
    {
        db.JobListings.Add(listing);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(JobListing listing, CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    // ── PATCH — partial update, only non-null fields applied ─────────────

    public async Task<JobResponse?> PatchAsync(
        Guid id, UpdateJobListingRequest request, CancellationToken ct = default)
    {
        // Load the tracked entity — the change tracker detects only what we mutate
        var listing = await db.JobListings.FindAsync([id], ct);
        if (listing is null) return null;

        // Apply only the fields that are non-null in the request
        if (request.Title          is not null) listing.Title       = request.Title;
        if (request.Description    is not null) listing.Description = request.Description;
        if (request.Location       is not null) listing.Location    = request.Location;
        if (request.EmploymentType is not null) listing.Type        = request.EmploymentType.Value;
        if (request.SalaryMin      is not null) listing.SalaryMin   = request.SalaryMin;
        if (request.SalaryMax      is not null) listing.SalaryMax   = request.SalaryMax;
        if (request.ClosingDate    is not null) listing.ClosingDate = request.ClosingDate.Value;

        // Re-validate salary range only if either salary field was included
        if (request.SalaryMin is not null || request.SalaryMax is not null)
        {
            if (listing.SalaryMin.HasValue && listing.SalaryMax.HasValue &&
                listing.SalaryMax <= listing.SalaryMin)
                throw new InvalidListingException(
                    "SalaryMax must be greater than SalaryMin.");
        }

        // Re-validate closing date only if it was included
        if (request.ClosingDate is not null && request.ClosingDate.Value <= DateTime.UtcNow)
            throw new InvalidListingException("Closing date must be in the future.");

        await db.SaveChangesAsync(ct);

        // Return the updated projection
        var detail = await GetDetailByIdAsync(id, ct);
        return detail is null ? null : new JobResponse(
            detail.id, detail.Title, detail.Description, detail.CompanyName,
            detail.Location, detail.Type, detail.SalaryMin, detail.SalaryMax,
            ComputeSalaryDisplay(detail.SalaryMin, detail.SalaryMax),
            detail.PostedAt, detail.IsActive, detail.Applications.Count(),
            detail.ClosingDate, detail.Status
        );
    }

    public async Task CloseAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await db.JobListings.FindAsync([id], ct);
        if (listing is null) return;
        listing.Status   = JobListingStatus.Closed;
        listing.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────────────

    private static string ComputeSalaryDisplay(decimal? min, decimal? max) =>
        (min, max) switch
        {
            (not null, not null) => $"R{min.Value:N0} – R{max.Value:N0}/month",
            (not null, null)     => $"From R{min.Value:N0}/month",
            _                    => "Salary not specified"
        };
}
