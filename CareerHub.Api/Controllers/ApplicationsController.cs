using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
    // ── POST /api/v1/applications/{listingId} — apply with rate limit ────
    [Authorize(Roles = "Applicant")]
    [HttpPost("{listingId:guid}")]
    [EnableRateLimiting("apply")]
    public async Task<ActionResult<ApplicationResponse>> ApplyAsync(
        Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        var response    = await applicationService.ApplyAsync(listingId, applicantId, ct);
        return Created($"/api/v1/applications/{listingId}", response);
    }

    // ── GET /api/v1/applications/listing/{listingId} — employer dashboard ──
    [Authorize(Roles = "Employer")]
    [HttpGet("listing/{listingId:guid}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetByListingAsync(
        Guid listingId, CancellationToken ct) =>
        Ok(await applicationService.GetByListingIdAsync(listingId, ct));

    // ── GET /api/v1/applications/my — applicant history with ETag ────────
    [Authorize(Roles = "Applicant")]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetMyApplicationsAsync(
        CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        return Ok(await applicationService.GetByApplicantIdAsync(applicantId, ct));
    }

    // ── PATCH /api/v1/applications/{listingId}/{applicantId}/status ───────
    // Employer advances an application through the review workflow.
    // Status transitions are validated by ApplicationStatusTransitions.
    // Illegal transitions return 422 Unprocessable Entity.
    [Authorize(Roles = "Employer")]
    [HttpPatch("{listingId:guid}/{applicantId:guid}/status")]
    public async Task<ActionResult> UpdateStatusAsync(
        Guid listingId,
        Guid applicantId,
        [FromBody] UpdateApplicationStatusRequest request,
        CancellationToken ct)
    {
        await applicationService.UpdateStatusAsync(listingId, applicantId, request.Status, ct);
        return NoContent();
    }

    // ── DELETE /api/v1/applications/{listingId} — applicant withdraws ────
    [Authorize(Roles = "Applicant")]
    [HttpDelete("{listingId:guid}")]
    public async Task<ActionResult> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        await applicationService.WithdrawAsync(listingId, applicantId, applicantId, ct);
        return NoContent();
    }
}
