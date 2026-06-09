using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    public async Task<ActionResult<IEnumerable<JobResponse>>> SearchAsync(
        [FromQuery] string q, CancellationToken ct) =>
        Ok(await jobService.SearchAsync(q, ct));

    [Authorize(Roles = "Employer")]
    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetStatsAsync(
        [FromQuery] Guid companyId, CancellationToken ct) =>
        Ok(await jobService.GetApplicationStatsAsync(companyId, ct));

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

    [HttpGet("{id:guid}")]
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
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var response = await jobService.CreateAsync(request, ct);
        return Created($"/api/v1/jobs/{response.id}", response);
    }

    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct) =>
        Ok(await jobService.UpdateAsync(id, request, ct));

    [Authorize(Roles = "Employer")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<JobResponse>> PatchJobAsync(
        Guid id, [FromBody] UpdateJobListingRequest request, CancellationToken ct) =>
        Ok(await jobService.PatchAsync(id, request, ct));

    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CloseJobAsync(Guid id, CancellationToken ct)
    {
        await jobService.CloseAsync(id, ct);
        return NoContent();
    }
}
