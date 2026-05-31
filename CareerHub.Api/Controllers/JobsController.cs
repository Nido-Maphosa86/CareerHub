using Microsoft.AspNetCore.Mvc;
using CareerHub.Api.Models;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{

    // ── GET /jobs ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync()
    {
        await Task.Delay(200); // stands in for: await _db.Jobs.ToListAsync()
        return Ok(JobListingStore.Jobs.Select(MapToResponse));
    }

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetJobByIdAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        // Controller no longer handles HTTP — it throws a domain exception.
        // GlobalExceptionHandler translates this to 404 Problem Details.
        if (job is null)
            throw new JobNotFoundException(id);

        return Ok(MapToResponse(job));
    }

    // ── POST /jobs ────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync([FromBody] CreateJobRequest request)
    {
        await Task.Delay(50);

        // Idempotency guard — throw instead of returning Conflict()
        bool isDuplicate = JobListingStore.Jobs.Any(j =>
            string.Equals(j.Title, request.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(j.Company, request.Company, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
            throw new DuplicateJobListingException(request.Title, request.Company);

        // Map DTO → domain model — server sets PostedAt and IsActive
        var newJob = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Company,
            request.Location,
            request.Type!.Value,
            request.SalaryMin,
            request.SalaryMax,
            DateTime.UtcNow,
            true
        );

        JobListingStore.Jobs.Add(newJob);

        var response = MapToResponse(newJob);

        return CreatedAtAction(nameof(GetJobByIdAsync), new { id = response.id }, response);
    }

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(Guid id, [FromBody] UpdateJobRequest request)
    {
        await Task.Delay(50);

        var existingJob = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        // Throw instead of return NotFound() — GlobalExceptionHandler handles it
        if (existingJob is null)
            throw new JobNotFoundException(id);

        var updatedJob = existingJob with
        {
            Title       = request.Title,
            Description = request.Description,
            Company     = request.Company,
            Location    = request.Location,
            Type        = request.Type!.Value,
            SalaryMin   = request.SalaryMin,
            SalaryMax   = request.SalaryMax
            // PostedAt and IsActive are preserved — not listed here
        };

        JobListingStore.Jobs.Remove(existingJob);
        JobListingStore.Jobs.Add(updatedJob);

        return Ok(MapToResponse(updatedJob));
    }

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteJobAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        // Throw instead of return NotFound() — GlobalExceptionHandler handles it
        if (job is null)
            throw new JobNotFoundException(id);

        JobListingStore.Jobs.Remove(job);

        return NoContent(); // 204 — success, nothing to return
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private static JobResponse MapToResponse(JobListing job) =>
        new(
            job.id,
            job.Title,
            job.Description,
            job.Company,
            job.Location,
            job.Type,
            job.SalaryMin,
            job.SalaryMax,
            ComputeSalaryDisplay(job.SalaryMin, job.SalaryMax),
            job.PostedAt,
            job.IsActive
        );

    private static string ComputeSalaryDisplay(decimal? min, decimal? max) =>
        (min, max) switch
        {
            (not null, not null) => $"R{min.Value:N0} – R{max.Value:N0}/month",
            (not null, null)     => $"From R{min.Value:N0}/month",
            _                    => "Salary not specified"
        };
}
