using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

// API versioning — URL segment: /api/v1/jobs
// AssumeDefaultVersionWhenUnspecified = true means /api/jobs also works (non-breaking)
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class JobsController(IJobListingService jobService) : ControllerBase
{
    // ── GET /api/v1/jobs ──────────────────────────────────────────────────
    // Paginated, filtered, sorted. Returns PagedResponse<JobResponse> envelope.
    // Writes X-Total-Count header so frontend can show totals without parsing body.
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobResponse>>> GetJobsAsync(
        [FromQuery] int     page           = 1,
        [FromQuery] int     pageSize       = 20,
        [FromQuery] string? location       = null,
        [FromQuery] string? employmentType = null,
        [FromQuery] decimal? salaryMin     = null,
        [FromQuery] decimal? salaryMax     = null,
        [FromQuery] Guid?   companyId      = null,
        [FromQuery] string  sort           = "postedAt",
        [FromQuery] string? dir            = null,
        CancellationToken ct               = default)
    {
        var filter   = new JobListingFilterQuery(location, employmentType, salaryMin, salaryMax, companyId, sort, dir);
        var result   = await jobService.GetActiveListingsAsync(page, pageSize, filter, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result);
    }

    // ── GET /api/v1/jobs/search?q={term} — full-text search with rate limit ──
    [HttpGet("search")]
    [EnableRateLimiting("search")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> SearchAsync(
        [FromQuery] string q, CancellationToken ct) =>
        Ok(await jobService.SearchAsync(q, ct));

    // ── GET /api/v1/jobs/stats?companyId={id} ────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetStatsAsync(
        [FromQuery] Guid companyId, CancellationToken ct) =>
        Ok(await jobService.GetApplicationStatsAsync(companyId, ct));

    // ── GET /api/v1/jobs/company/{companyId} — employer's own listings ───
    [Authorize(Roles = "Employer")]
    [HttpGet("company/{companyId:guid}")]
    public async Task<ActionResult<PagedResponse<JobResponse>>> GetCompanyJobsAsync(
        Guid companyId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct     = default)
    {
        var result = await jobService.GetCompanyListingsAsync(companyId, page, pageSize, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result);
    }

    // ── GET /api/v1/jobs/{id} — ETag conditional response ────────────────
    // Returns 304 Not Modified if the listing has not changed since the client's last request.
    // ETag is computed from ID + PostedAt ticks + SalaryMin — changes whenever salary changes.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobByIdAsync(Guid id, CancellationToken ct)
    {
        var listing = await jobService.GetByIdAsync(id, ct);

        // ETag fingerprint: ID + PostedAt ticks + SalaryMin
        // Limitation: changes to Description or Location do not change this ETag.
        // A stronger ETag would use a LastModifiedAt timestamp updated on every save.
        var etag = $"\"{listing.id}-{listing.PostedAt.Ticks}-{listing.SalaryMin}\"";

        // If client's cached ETag matches — return 304, no body, no serialisation cost
        if (Request.Headers.IfNoneMatch == etag)
            return StatusCode(304);

        Response.Headers.ETag = etag;
        return Ok(listing);
    }

    // ── POST /api/v1/jobs ─────────────────────────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpPost]
    [EnableRateLimiting("post-listing")]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var response = await jobService.CreateAsync(request, ct);
        return Created($"/api/v1/jobs/{response.id}", response);
    }

    // ── PUT /api/v1/jobs/{id} — full replacement ─────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct) =>
        Ok(await jobService.UpdateAsync(id, request, ct));

    // ── PATCH /api/v1/jobs/{id} — partial update ─────────────────────────
    // Only non-null fields in the request body are applied.
    // Resolves the PUT race condition: two recruiters updating different fields
    // at the same time no longer overwrite each other's work.
    [Authorize(Roles = "Employer")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<JobResponse>> PatchJobAsync(
        Guid id, [FromBody] UpdateJobListingRequest request, CancellationToken ct) =>
        Ok(await jobService.PatchAsync(id, request, ct));

    // ── DELETE /api/v1/jobs/{id} — closes the listing ────────────────────
    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CloseJobAsync(Guid id, CancellationToken ct)
    {
        await jobService.CloseAsync(id, ct);
        return NoContent();
    }
}
