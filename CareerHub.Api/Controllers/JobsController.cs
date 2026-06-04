using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerHub.Api.Models;
using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;

namespace CareerHub.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController(CareerHubDbContext db) : ControllerBase
{

    // ── GET /jobs ─────────────────────────────────────────────────────────
    // Anonymous — no token required.
    //
    // N+1 FIX: We use a projection (Select) instead of Include.
    // Without a fix, EF Core would fire:
    //   1 query for all job listings
    //   + N queries — one per listing to load the company name
    //   + N queries — one per listing to count applications
    //   = 1 + 2N queries total
    //
    // With projection, everything is fetched in ONE SQL statement:
    //   SELECT j.id, j.title, c.name AS company_name, COUNT(a.job_listing_id)
    //   FROM job_listings j
    //   LEFT JOIN companies c ON j.company_id = c.id
    //   LEFT JOIN applications a ON a.job_listing_id = j.id
    //   GROUP BY j.id, c.name
    //
    // AsNoTracking() — this is a read-only endpoint. We never call
    // SaveChangesAsync() here, so we do not need the change tracker.
    // Skipping it saves memory and CPU.

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync(
        CancellationToken cancellationToken)
    {
        // Step 1: Project to an anonymous type — only the columns we need.
        // ApplicationCount is computed by the database (COUNT(*)), not in C#.
        var rawJobs = await db.JobListings
            .AsNoTracking()
            .OrderByDescending(j => j.PostedAt)
            .Select(j => new
            {
                j.Id,
                j.Title,
                j.Description,
                CompanyName      = j.Company.Name,  // JOIN to companies, only Name column
                j.Location,
                j.Type,
                j.SalaryMin,
                j.SalaryMax,
                j.PostedAt,
                j.IsActive,
                ApplicationCount = j.Applications.Count()  // COUNT in SQL
            })
            .ToListAsync(cancellationToken);

        // Step 2: Map to the response DTO in C# (SalaryDisplay is computed here)
        return Ok(rawJobs.Select(j => new JobResponse(
            j.Id, j.Title, j.Description, j.CompanyName,
            j.Location, j.Type, j.SalaryMin, j.SalaryMax,
            ComputeSalaryDisplay(j.SalaryMin, j.SalaryMax),
            j.PostedAt, j.IsActive, j.ApplicationCount
        )));
    }

    // ── GET /jobs/{id} ────────────────────────────────────────────────────
    // Anonymous — returns full detail including applications received.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailResponse>> GetJobByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // Projection: fetch only the columns needed by JobDetailResponse.
        // The nested Select on Applications gives us applicant name + status —
        // not the full Applicant entity (no email, no other fields).
        var raw = await db.JobListings
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new
            {
                j.Id, j.Title, j.Description,
                CompanyName  = j.Company.Name,
                j.Location, j.Type, j.SalaryMin, j.SalaryMax,
                j.PostedAt, j.IsActive,
                Applications = j.Applications.Select(a => new
                {
                    ApplicantName = a.Applicant.Name,
                    a.SubmittedAt,
                    a.Status
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
            throw new JobNotFoundException(id);

        return Ok(new JobDetailResponse(
            raw.Id, raw.Title, raw.Description, raw.CompanyName,
            raw.Location, raw.Type, raw.SalaryMin, raw.SalaryMax,
            ComputeSalaryDisplay(raw.SalaryMin, raw.SalaryMax),
            raw.PostedAt, raw.IsActive,
            raw.Applications.Select(a => new ApplicationSummary(
                a.ApplicantName,
                a.SubmittedAt,
                a.Status.ToString()
            ))
        ));
    }

    // ── POST /jobs ────────────────────────────────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> CreateJobAsync(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken)
    {
        // Verify the company exists
        var company = await db.Companies.FindAsync([request.CompanyId!.Value], cancellationToken);
        if (company is null)
            throw new CompanyNotFoundException(request.CompanyId.Value);

        // Idempotency: same title + same company = duplicate
        bool isDuplicate = await db.JobListings.AnyAsync(j =>
            j.Title.ToLower() == request.Title.ToLower() &&
            j.CompanyId == request.CompanyId, cancellationToken);

        if (isDuplicate)
            throw new DuplicateJobListingException(request.Title, company.Name);

        var newJob = new JobListing(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.CompanyId.Value,
            request.Location,
            request.Type!.Value,
            request.SalaryMin,
            request.SalaryMax,
            DateTime.UtcNow,
            true
        );

        db.JobListings.Add(newJob);
        await db.SaveChangesAsync(cancellationToken);

        // Map to response — need company name for the DTO
        var response = new JobResponse(
            newJob.Id, newJob.Title, newJob.Description, company.Name,
            newJob.Location, newJob.Type, newJob.SalaryMin, newJob.SalaryMax,
            ComputeSalaryDisplay(newJob.SalaryMin, newJob.SalaryMax),
            newJob.PostedAt, newJob.IsActive, 0
        );

        return Created($"/jobs/{response.id}", response);
    }

    // ── PUT /jobs/{id} ────────────────────────────────────────────────────
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

        var company = await db.Companies.FindAsync([request.CompanyId!.Value], cancellationToken);
        if (company is null)
            throw new CompanyNotFoundException(request.CompanyId.Value);

        existingJob.Title       = request.Title;
        existingJob.Description = request.Description;
        existingJob.CompanyId   = request.CompanyId.Value;
        existingJob.Location    = request.Location;
        existingJob.Type        = request.Type!.Value;
        existingJob.SalaryMin   = request.SalaryMin;
        existingJob.SalaryMax   = request.SalaryMax;

        await db.SaveChangesAsync(cancellationToken);

        var response = new JobResponse(
            existingJob.Id, existingJob.Title, existingJob.Description, company.Name,
            existingJob.Location, existingJob.Type, existingJob.SalaryMin, existingJob.SalaryMax,
            ComputeSalaryDisplay(existingJob.SalaryMin, existingJob.SalaryMax),
            existingJob.PostedAt, existingJob.IsActive, 0
        );

        return Ok(response);
    }

    // ── DELETE /jobs/{id} ─────────────────────────────────────────────────
    [Authorize(Roles = "Employer")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteJobAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var job = await db.JobListings.FindAsync([id], cancellationToken);
        if (job is null)
            throw new JobNotFoundException(id);

        db.JobListings.Remove(job);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // ── POST /jobs/{id}/apply ─────────────────────────────────────────────
    // Applicants submit an application for a specific job listing.
    [Authorize(Roles = "Applicant")]
    [HttpPost("{id:guid}/apply")]
    public async Task<ActionResult> ApplyForJobAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        // Read applicant ID from the JWT claim set by AuthController at login
        var applicantIdStr = User.FindFirstValue("ApplicantId")!;
        var applicantId    = Guid.Parse(applicantIdStr);

        // Verify the job exists
        bool jobExists = await db.JobListings.AnyAsync(j => j.Id == id, cancellationToken);
        if (!jobExists)
            throw new JobNotFoundException(id);

        // Check for duplicate application — the composite PK prevents this at DB level too,
        // but we throw a domain exception here so GlobalExceptionHandler returns a clean 409.
        bool alreadyApplied = await db.Applications.AnyAsync(a =>
            a.JobListingId == id && a.ApplicantId == applicantId, cancellationToken);

        if (alreadyApplied)
            throw new DuplicateApplicationException(id);

        var application = new Application
        {
            JobListingId = id,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow,
            Status       = ApplicationStatus.Submitted
        };

        db.Applications.Add(application);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/jobs/{id}", new
        {
            Message     = "Application submitted successfully.",
            JobListingId = id,
            ApplicantId  = applicantId,
            SubmittedAt  = application.SubmittedAt,
            Status       = application.Status.ToString()
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private static string ComputeSalaryDisplay(decimal? min, decimal? max) =>
        (min, max) switch
        {
            (not null, not null) => $"R{min.Value:N0} – R{max.Value:N0}/month",
            (not null, null)     => $"From R{min.Value:N0}/month",
            _                    => "Salary not specified"
        };
}
