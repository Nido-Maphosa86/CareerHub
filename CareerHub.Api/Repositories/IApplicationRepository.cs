using CareerHub.Api.DTOs;
using CareerHub.Api.Models;

namespace CareerHub.Api.Repositories;

public interface IApplicationRepository
{
    Task<bool> HasAlreadyAppliedAsync(Guid listingId, Guid applicantId, CancellationToken ct = default);

    Task<IEnumerable<ApplicationResponse>> GetByListingIdAsync(Guid listingId, CancellationToken ct = default);

    Task<IEnumerable<ApplicationResponse>> GetByApplicantIdAsync(Guid applicantId, CancellationToken ct = default);

    Task<Application?> GetEntityAsync(Guid listingId, Guid applicantId, CancellationToken ct = default);

    Task AddAsync(Application application, CancellationToken ct = default);

    Task UpdateStatusAsync(Application application, ApplicationStatus newStatus, CancellationToken ct = default);

    Task DeleteAsync(Application application, CancellationToken ct = default);
}