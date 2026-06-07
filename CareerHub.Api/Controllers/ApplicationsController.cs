using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

// This controller was extracted from JobsController in Assignment 2.3.
// In 2.2, POST /jobs/{id}/apply lived in JobsController alongside listing CRUD.
// Applications are a separate domain concern — they belong in their own controller.

[ApiController]
[Route("[controller]")]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
    // ── POST /applications/{listingId} — applicant applies ────────────────
    [Authorize(Roles = "Applicant")]
    [HttpPost("{listingId:guid}")]
    public async Task<ActionResult<ApplicationResponse>> ApplyAsync(Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        var response    = await applicationService.ApplyAsync(listingId, applicantId, ct);
        return Created($"/applications/{listingId}", response);
    }

    // ── GET /applications/listing/{listingId} — employer dashboard ────────
    [Authorize(Roles = "Employer")]
    [HttpGet("listing/{listingId:guid}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetByListingAsync(
        Guid listingId, CancellationToken ct) =>
        Ok(await applicationService.GetByListingIdAsync(listingId, ct));

    // ── GET /applications/my — applicant sees their own history ───────────
    [Authorize(Roles = "Applicant")]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetMyApplicationsAsync(CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        return Ok(await applicationService.GetByApplicantIdAsync(applicantId, ct));
    }

    // ── PUT /applications/{listingId}/{applicantId}/status — employer updates ──
    [Authorize(Roles = "Employer")]
    [HttpPut("{listingId:guid}/{applicantId:guid}/status")]
    public async Task<ActionResult> UpdateStatusAsync(
        Guid listingId, Guid applicantId,
        [FromBody] UpdateApplicationStatusRequest request,
        CancellationToken ct)
    {
        await applicationService.UpdateStatusAsync(listingId, applicantId, request.Status, ct);
        return NoContent();
    }

    // ── DELETE /applications/{listingId} — applicant withdraws ───────────
    [Authorize(Roles = "Applicant")]
    [HttpDelete("{listingId:guid}")]
    public async Task<ActionResult> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        await applicationService.WithdrawAsync(listingId, applicantId, applicantId, ct);
        return NoContent();
    }
}
