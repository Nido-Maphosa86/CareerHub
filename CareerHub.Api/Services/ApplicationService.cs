using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;

namespace CareerHub.Api.Services;

// ── Interface ────────────────────────────────────────────────────────────────

public interface IApplicationService
{
    Task<ApplicationResponse> ApplyAsync(Guid listingId, Guid applicantId, CancellationToken ct = default);
    Task<IEnumerable<ApplicationResponse>> GetByListingIdAsync(Guid listingId, CancellationToken ct = default);
    Task<IEnumerable<ApplicationResponse>> GetByApplicantIdAsync(Guid applicantId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid listingId, Guid applicantId, ApplicationStatus newStatus, CancellationToken ct = default);
    Task WithdrawAsync(Guid listingId, Guid applicantId, Guid requestingApplicantId, CancellationToken ct = default);
}

// ── Implementation ───────────────────────────────────────────────────────────

// No Microsoft.EntityFrameworkCore imports — all persistence happens in the repository.
// All business rules enforced here — controllers are left with HTTP-only concerns.

public class ApplicationService(
    IApplicationRepository appRepo,
    IJobListingRepository  listingRepo) : IApplicationService
{
    public async Task<ApplicationResponse> ApplyAsync(
        Guid listingId, Guid applicantId, CancellationToken ct = default)
    {
        // Rule 1: The listing must be open for applications.
        // IsOpenForApplicationsAsync checks Status = Active AND ClosingDate > now.
        // Wrong choice: doing this check in the controller would require the controller
        // to call a repository directly — breaking the layer boundary.
        if (!await listingRepo.IsOpenForApplicationsAsync(listingId, ct))
            throw new ListingClosedException(listingId);

        // Rule 2: An applicant cannot apply twice to the same listing.
        // The composite PK enforces this at the database level too,
        // but we throw a domain exception here for a clean 409 response.
        if (await appRepo.HasAlreadyAppliedAsync(listingId, applicantId, ct))
            throw new DuplicateApplicationException(listingId);

        var application = new Application
        {
            JobListingId = listingId,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow,
            Status       = ApplicationStatus.Submitted
        };

        await appRepo.AddAsync(application, ct);

        // Return a response by fetching the projection from the repository
        var all = await appRepo.GetByApplicantIdAsync(applicantId, ct);
        return all.First(a => a.JobListingId == listingId);
    }

    public Task<IEnumerable<ApplicationResponse>> GetByListingIdAsync(
        Guid listingId, CancellationToken ct = default) =>
        appRepo.GetByListingIdAsync(listingId, ct);

    public Task<IEnumerable<ApplicationResponse>> GetByApplicantIdAsync(
        Guid applicantId, CancellationToken ct = default) =>
        appRepo.GetByApplicantIdAsync(applicantId, ct);

    public async Task UpdateStatusAsync(
        Guid listingId, Guid applicantId, ApplicationStatus newStatus, CancellationToken ct = default)
    {
        var application = await appRepo.GetEntityAsync(listingId, applicantId, ct);

        if (application is null)
            throw new DuplicateApplicationException(listingId); // re-using as "not found"

        // Rule 3: Status transitions must follow the valid workflow.
        // ApplicationStatusTransitions is a pure function — no database query needed.
        // This method can be unit-tested completely independently of the database.
        if (!ApplicationStatusTransitions.IsValid(application.Status, newStatus))
            throw new InvalidStatusTransitionException(
                application.Status.ToString(), newStatus.ToString());

        await appRepo.UpdateStatusAsync(application, newStatus, ct);
    }

    public async Task WithdrawAsync(
        Guid listingId, Guid applicantId, Guid requestingApplicantId, CancellationToken ct = default)
    {
        // Rule 4: An applicant can only withdraw their own application.
        // Wrong choice: checking this in the controller means the controller
        // is enforcing a business rule — that is not the controller's job.
        if (applicantId != requestingApplicantId)
            throw new UnauthorizedOperationException(
                "You can only withdraw your own application.");

        var application = await appRepo.GetEntityAsync(listingId, applicantId, ct);

        if (application is null)
            throw new DuplicateApplicationException(listingId);

        await appRepo.DeleteAsync(application, ct);
    }
}
