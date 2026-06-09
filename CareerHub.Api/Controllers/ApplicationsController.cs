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
[Route("api/[controller]")]
public class ApplicationsController(IApplicationService applicationService) : ControllerBase
{
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

    [Authorize(Roles = "Employer")]
    [HttpGet("listing/{listingId:guid}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetByListingAsync(
        Guid listingId, CancellationToken ct) =>
        Ok(await applicationService.GetByListingIdAsync(listingId, ct));

    [Authorize(Roles = "Applicant")]
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetMyApplicationsAsync(
        CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        return Ok(await applicationService.GetByApplicantIdAsync(applicantId, ct));
    }

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

    [Authorize(Roles = "Applicant")]
    [HttpDelete("{listingId:guid}")]
    public async Task<ActionResult> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        await applicationService.WithdrawAsync(listingId, applicantId, applicantId, ct);
        return NoContent();
    }
}
