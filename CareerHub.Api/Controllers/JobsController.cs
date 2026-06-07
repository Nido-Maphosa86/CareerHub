using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

// WHAT MOVED OUT OF THIS CONTROLLER (Assignment 2.3 refactor):
// - Company existence check → JobListingService.CreateAsync
// - Closing date validation → JobListingService.CreateAsync
// - Duplicate listing check → JobListingService (via JobListingRepository)
// - AsNoTracking, ToListAsync, AnyAsync, FindAsync → JobListingRepository
// - Domain entity construction → JobListingService.CreateAsync
// - MapToResponse / ComputeSalaryDisplay → JobListingRepository projection
//
// WHAT STAYS:
// - [Authorize] attributes — HTTP concern (who can call this endpoint)
// - [HttpGet/Post/Put/Delete] — HTTP verb mapping
// - Return statements — HTTP response shape
// - CancellationToken — HTTP request lifecycle

[ApiController]
[Route("[controller]")]
public class JobsController(IJobListingService jobService) : ControllerBase
{
    // ── GET /jobs ─────────────────────────────────────────────────────────
    // Three things: parse request, call service, return response.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync(CancellationToken ct) =>
        Ok(await jobService.GetActiveListingsAsync(ct));

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> GetJobByIdAsync(Guid id, CancellationToken ct) =>
        Ok(await jobService.GetByIdAsync(id, ct));

    // ── POST /jobs ────────────────────────────────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var response = await jobService.CreateAsync(request, ct);
        return Created($"/jobs/{response.id}", response);
    }

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct) =>
        Ok(await jobService.UpdateAsync(id, request, ct));

    // ── DELETE /jobs/{id} — closes the listing ────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CloseJobAsync(Guid id, CancellationToken ct)
    {
        await jobService.CloseAsync(id, ct);
        return NoContent();
    }
}
