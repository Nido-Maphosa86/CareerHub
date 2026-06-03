using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CareerHub.Api.Models;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController(CareerHubDbContext db) : ControllerBase
{
    // db is injected by the DI container — same pattern as class BookingsController(BookingDbContext db)
    // Registered as Scoped — one instance per HTTP request, then disposed.

    // ── GET /jobs ─────────────────────────────────────────────────────────
    // Anonymous — no token required
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync(
        CancellationToken cancellationToken)
    {
        // ToListAsync() translates to: SELECT * FROM job_listings ORDER BY posted_at DESC
        // Returns an empty list — not a 404 — when no jobs exist yet
        var jobs = await db.JobListings
            .OrderByDescending(j => j.PostedAt)
            .ToListAsync(cancellationToken);

        return Ok(jobs.Select(MapToResponse));
    }

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    // Anonymous — no token required
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetJobByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // FindAsync checks the change tracker first (in case the entity was
        // already loaded this request), then hits the database.
        // More efficient than FirstOrDefaultAsync for primary key lookups.
        var job = await db.JobListings.FindAsync([id], cancellationToken);

        if (job is null)
            throw new JobNotFoundException(id);

        return Ok(MapToResponse(job));
    }

    // ── POST /jobs ────────────────────────────────────────────────────────
    // Requires a valid JWT with the Employer role
    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        // AnyAsync generates an EXISTS query in SQL —
        // more efficient than loading the full entity just to check existence
        bool isDuplicate = await db.JobListings.AnyAsync(j =>
            j.Title.ToLower() == request.Title.ToLower() &&
            j.Company.ToLower() == request.Company.ToLower(),
            cancellationToken);

        if (isDuplicate)
            throw new DuplicateJobListingException(request.Title, request.Company);

        // Map DTO → entity using the constructor — same pattern as class code.
        // Server sets PostedAt and IsActive — client never supplies these.
        // We generate the Guid here so we know the ID before saving —
        // this lets us build the 201 Location header immediately.
        var newJob = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Company,
            request.Location,
            request.Type!.Value,
            request.SalaryMin,
            request.SalaryMax,
            DateTime.UtcNow, // server owns this
            true             // server owns this — active by default
        );

        // Add() only updates the change tracker — no database write yet
        db.JobListings.Add(newJob);

        // SaveChangesAsync() is where the INSERT statement runs
        await db.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(newJob);

        return Created($"/jobs/{response.id}", response);
    }

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
    // Requires a valid JWT with the Employer role
    [Authorize(Roles = "Employer")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> UpdateJobAsync(
        Guid id,
        [FromBody] UpdateJobRequest request,
        CancellationToken cancellationToken)
    {
        var existingJob = await db.JobListings.FindAsync([id], cancellationToken);

        if (existingJob is null)
            throw new JobNotFoundException(id);

        // Mutate the properties directly on the tracked entity —
        // same pattern as class code (existingBooking.Title = request.Title)
        // EF Core's change tracker has a snapshot of the original values.
        // SaveChangesAsync() compares current vs snapshot and generates
        // a targeted UPDATE for only the changed columns.
        // PostedAt and IsActive are intentionally not touched here —
        // a PUT must not reset server-owned fields.
        existingJob.Title       = request.Title;
        existingJob.Description = request.Description;
        existingJob.Company     = request.Company;
        existingJob.Location    = request.Location;
        existingJob.Type        = request.Type!.Value;
        existingJob.SalaryMin   = request.SalaryMin;
        existingJob.SalaryMax   = request.SalaryMax;

        // One SaveChangesAsync at the end — not once per property.
        // The change tracker batches all mutations into a single UPDATE.
        await db.SaveChangesAsync(cancellationToken);

        return Ok(MapToResponse(existingJob));
    }

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    // Requires a valid JWT with the Employer role
    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteJobAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await db.JobListings.FindAsync([id], cancellationToken);

        if (job is null)
            throw new JobNotFoundException(id);

        // Remove() marks the entity for deletion in the change tracker
        db.JobListings.Remove(job);

        // The DELETE statement runs here
        await db.SaveChangesAsync(cancellationToken);

        return NoContent(); // 204 — success, nothing to return
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private static JobResponse MapToResponse(JobListing job) =>
        new(
            job.Id,
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
