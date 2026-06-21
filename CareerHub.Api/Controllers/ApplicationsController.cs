using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [EndpointSummary("Apply to a job listing")]
    [EndpointDescription(
        "Submits an application from the logged-in applicant to the given job listing. " +
        "Requires the Applicant role. Rate limited to stop bot-driven mass applications. " +
        "Returns a conflict if the applicant has already applied to this listing.")]
    public async Task<ActionResult<ApplicationResponse>> ApplyAsync(
        Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        var response    = await applicationService.ApplyAsync(listingId, applicantId, ct);
        return Created($"/api/v1/applications/{listingId}", response);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("listing/{listingId:guid}")]
    [EndpointSummary("List applications for a job listing")]
    [EndpointDescription(
        "Returns every application submitted to the given job listing. Requires the Employer role.")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetByListingAsync(
        Guid listingId, CancellationToken ct) =>
        Ok(await applicationService.GetByListingIdAsync(listingId, ct));

    [Authorize(Roles = "Applicant")]
    [HttpGet("my")]
    [EndpointSummary("List my applications")]
    [EndpointDescription(
        "Returns every application submitted by the logged-in applicant. Requires the Applicant role.")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetMyApplicationsAsync(
        CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        return Ok(await applicationService.GetByApplicantIdAsync(applicantId, ct));
    }

    [Authorize(Roles = "Employer")]
    [HttpPatch("{listingId:guid}/{applicantId:guid}/status")]
    [EndpointSummary("Update an application's status")]
    [EndpointDescription(
        "Moves an application to a new status (for example Reviewed, Shortlisted, Rejected). " +
        "Requires the Employer role. Only certain status transitions are allowed — see " +
        "ApplicationStatusTransitions for the full set of legal moves.")]
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
    [EndpointSummary("Withdraw an application")]
    [EndpointDescription(
        "Withdraws the logged-in applicant's own application to the given job listing. " +
        "Requires the Applicant role.")]
    public async Task<ActionResult> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        var applicantId = Guid.Parse(User.FindFirstValue("ApplicantId")!);
        await applicationService.WithdrawAsync(listingId, applicantId, applicantId, ct);
        return NoContent();
    }
}
