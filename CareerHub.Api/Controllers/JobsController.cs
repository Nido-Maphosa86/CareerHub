using Microsoft.AspNetCore.Mvc;
using CareerHub.Api.Models;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;

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
    // The ":guid" constraint means the framework rejects non-GUID segments
    // with a 400 before our code even runs.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetJobByIdAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        // Guard clause: never return null. Never return 200 with an empty body.
        // 404 tells the client exactly what happened.
        if (job is null)
            return NotFound(); // HTTP 404 Not Found

        return Ok(MapToResponse(job)); // HTTP 200 OK
    }

    // ── POST /jobs ────────────────────────────────────────────────────────
    // [ApiController] runs [Required] and [Range] validation automatically.
    // IValidatableObject on CreateJobRequest handles the SalaryMax > SalaryMin check.
    // Neither check needs any code here — the controller stays clean.
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync([FromBody] CreateJobRequest request)
    {
        await Task.Delay(50);

        // 1. Idempotency guard — reject if same title + company already exists.
        //    Case-insensitive so "Software Engineer" and "software engineer" are the same job.
        bool isDuplicate = JobListingStore.Jobs.Any(j =>
            string.Equals(j.Title, request.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(j.Company, request.Company, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
            return Conflict(); // HTTP 409 Conflict — ProblemDetails fills in the body

        // 2. Map DTO → domain model.
        //    Server sets PostedAt and IsActive — client never supplies these.
        var newJob = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Company,
            request.Location,
            request.Type!.Value,    // Type is JobType? with [Required] — safe to unwrap here
            request.SalaryMin,
            request.SalaryMax,
            DateTime.UtcNow,        // server owns this
            true                    // server owns this — active by default
        );

        // 3. Save
        JobListingStore.Jobs.Add(newJob);

        // 4. Map domain model → response DTO
        var response = MapToResponse(newJob);

        // 5. Return 201 Created with a Location header pointing to GET /jobs/{id}
        return CreatedAtAction(nameof(GetJobByIdAsync), new { id = response.id }, response);
    }

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
    // Fully replaces editable fields. PostedAt and IsActive are preserved —
    // a PUT must never reset server-owned values.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(Guid id, [FromBody] UpdateJobRequest request)
    {
        await Task.Delay(50);

        var existingJob = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        if (existingJob is null)
            return NotFound(); // HTTP 404

        // `with` expression creates a new record, copying all unchanged fields.
        // PostedAt and IsActive are not listed here — they carry over untouched.
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

        // Replace in store
        JobListingStore.Jobs.Remove(existingJob);
        JobListingStore.Jobs.Add(updatedJob);

        return Ok(MapToResponse(updatedJob)); // HTTP 200 OK with updated body
    }

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteJobAsync(Guid id)
    {
        await Task.Delay(50);

        var job = JobListingStore.Jobs.FirstOrDefault(j => j.id == id);

        if (job is null)
            return NotFound(); // HTTP 404 — resource does not exist

        JobListingStore.Jobs.Remove(job);

        return NoContent(); // HTTP 204 — success, nothing to return
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    // Maps a domain model to the response DTO, computing SalaryDisplay inline.
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

    // Produces a human-readable salary string — never stored, always computed.
    private static string ComputeSalaryDisplay(decimal? min, decimal? max) =>
        (min, max) switch
        {
            (not null, not null) => $"R{min.Value:N0} – R{max.Value:N0}/month",
            (not null, null)     => $"From R{min.Value:N0}/month",
            _                    => "Salary not specified"
        };
}
