using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class JobsController(IJobListingService jobService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List active job listings")]
    [EndpointDescription(
        "Returns a paged list of active job listings. Supports filtering by location, " +
        "employment type, salary range, and company, plus sorting. " +
        "The total result count is returned in the X-Total-Count response header.")]
    public async Task<ActionResult<PagedResponse<JobResponse>>> GetJobsAsync(
        [FromQuery] int      page           = 1,
        [FromQuery] int      pageSize       = 20,
        [FromQuery] string?  location       = null,
        [FromQuery] string?  employmentType = null,
        [FromQuery] decimal? salaryMin      = null,
        [FromQuery] decimal? salaryMax      = null,
        [FromQuery] Guid?    companyId      = null,
        [FromQuery] string   sort           = "postedAt",
        [FromQuery] string?  dir            = null,
        CancellationToken    ct             = default)
    {
        var filter = new JobListingFilterQuery(location, employmentType, salaryMin, salaryMax, companyId, sort, dir);
        var result = await jobService.GetActiveListingsAsync(page, pageSize, filter, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result);
    }

    [HttpGet("search")]
    [EnableRateLimiting("search")]
    [EndpointSummary("Full text search job listings")]
    [EndpointDescription(
        "Searches job titles and descriptions using PostgreSQL full text search. " +
        "Rate limited more tightly than other endpoints to protect the search index from abuse.")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> SearchAsync(
        [FromQuery] string q, CancellationToken ct) =>
        Ok(await jobService.SearchAsync(q, ct));

    [Authorize(Roles = "Employer")]
    [HttpGet("stats")]
    [EndpointSummary("Get application statistics for a company's listings")]
    [EndpointDescription(
        "Returns application counts per job listing for the given company. " +
        "Requires the Employer role. Used by employers to see which of their postings " +
        "are attracting applicants.")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetStatsAsync(
        [FromQuery] Guid companyId, CancellationToken ct) =>
        Ok(await jobService.GetApplicationStatsAsync(companyId, ct));

    [Authorize(Roles = "Employer")]
    [HttpGet("company/{companyId:guid}")]
    [EndpointSummary("List a company's job listings")]
    [EndpointDescription(
        "Returns a paged list of job listings belonging to the given company, including " +
        "closed listings. Requires the Employer role.")]
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

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a single job listing")]
    [EndpointDescription(
        "Returns one job listing by id. Supports ETag caching — if the If-None-Match header " +
        "matches the current ETag, returns 304 Not Modified instead of the full body.")]
    public async Task<IActionResult> GetJobByIdAsync(Guid id, CancellationToken ct)
    {
        var listing = await jobService.GetByIdAsync(id, ct);

        var etag = $"\"{listing.id}-{listing.PostedAt.Ticks}-{listing.SalaryMin}\"";

        if (Request.Headers.IfNoneMatch == etag)
            return StatusCode(304);

        Response.Headers.ETag = etag;
        return Ok(listing);
    }

    [Authorize(Roles = "Employer")]
    [HttpPost]
    [EnableRateLimiting("post-listing")]
    [EndpointSummary("Create a job listing")]
    [EndpointDescription(
        "Creates a new active job listing for the employer's company. Requires the Employer role. " +
        "Rate limited to prevent rapid bulk posting.")]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var response = await jobService.CreateAsync(request, ct);
        return Created($"/api/v1/jobs/{response.id}", response);
    }

    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    [EndpointSummary("Replace a job listing")]
    [EndpointDescription(
        "Replaces every field on an existing job listing. Requires the Employer role. " +
        "Use PATCH instead if you only need to change a few fields.")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct) =>
        Ok(await jobService.UpdateAsync(id, request, ct));

    [Authorize(Roles = "Employer")]
    [HttpPatch("{id:guid}")]
    [EndpointSummary("Partially update a job listing")]
    [EndpointDescription(
        "Updates only the fields supplied in the request body, leaving the rest unchanged. " +
        "Requires the Employer role.")]
    public async Task<ActionResult<JobResponse>> PatchJobAsync(
        Guid id, [FromBody] UpdateJobListingRequest request, CancellationToken ct) =>
        Ok(await jobService.PatchAsync(id, request, ct));

    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Close a job listing")]
    [EndpointDescription(
        "Closes a job listing so it no longer accepts applications and drops out of the active " +
        "listings list. Does not delete the listing or its application history. Requires the Employer role.")]
    public async Task<ActionResult> CloseJobAsync(Guid id, CancellationToken ct)
    {
        await jobService.CloseAsync(id, ct);
        return NoContent();
    }
}
