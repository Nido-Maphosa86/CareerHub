using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

// WHAT CHANGED FROM 2.3:
// - GET /jobs/search?q={term} added — calls service.SearchAsync (Part 5)
// - GET /jobs/stats?companyId={id} added — calls service.GetApplicationStatsAsync (Part 8)
// Every action is exactly 1 line — parse request, call service, return response.

[ApiController]
[Route("[controller]")]
public class JobsController(IJobListingService jobService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync(CancellationToken ct) =>
        Ok(await jobService.GetActiveListingsAsync(ct));

    // Full-text search using GIN index on tsvector column
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<JobResponse>>> SearchAsync(
        [FromQuery] string q, CancellationToken ct) =>
        Ok(await jobService.SearchAsync(q, ct));

    // Application statistics per listing using RANK() window function
    [Authorize(Roles = "Employer")]
    [HttpGet("stats")]
    public async Task<ActionResult<IEnumerable<JobListingStatsResponse>>> GetStatsAsync(
        [FromQuery] Guid companyId, CancellationToken ct) =>
        Ok(await jobService.GetApplicationStatsAsync(companyId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> GetJobByIdAsync(Guid id, CancellationToken ct) =>
        Ok(await jobService.GetByIdAsync(id, ct));

    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var response = await jobService.CreateAsync(request, ct);
        return Created($"/jobs/{response.id}", response);
    }

    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct) =>
        Ok(await jobService.UpdateAsync(id, request, ct));

    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CloseJobAsync(Guid id, CancellationToken ct)
    {
        await jobService.CloseAsync(id, ct);
        return NoContent();
    }
}
