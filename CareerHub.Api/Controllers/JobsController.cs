using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    // Anonymous — no token required. Public read access stays open.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync()
    {
        await Task.Delay(200);
        return Ok(JobListingStore.Jobs.Select(MapToResponse));
    }

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    // Anonymous — no token required.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetJobByIdAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        if (job is null)
            throw new JobNotFoundException(id);

        return Ok(MapToResponse(job));
    }

    // ── POST /jobs ────────────────────────────────────────────────────────
    // Requires a valid JWT with the Employer role.
    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync([FromBody] CreateJobRequest request)
    {
        await Task.Delay(50);

        bool isDuplicate = JobListingStore.Jobs.Any(j =>
            string.Equals(j.Title, request.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(j.Company, request.Company, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
            throw new DuplicateJobListingException(request.Title, request.Company);

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
    // Requires a valid JWT with the Employer role.
    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(Guid id, [FromBody] UpdateJobRequest request)
    {
        await Task.Delay(50);

        var existingJob = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

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
        };

        JobListingStore.Jobs.Remove(existingJob);
        JobListingStore.Jobs.Add(updatedJob);

        return Ok(MapToResponse(updatedJob));
    }

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    // Requires a valid JWT with the Employer role.
    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteJobAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        if (job is null)
            throw new JobNotFoundException(id);

        JobListingStore.Jobs.Remove(job);

        return NoContent();
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
