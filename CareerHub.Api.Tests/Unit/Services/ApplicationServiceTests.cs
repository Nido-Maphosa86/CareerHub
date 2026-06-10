using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using NSubstitute;

namespace CareerHub.Api.Tests.Unit.Services;

public class ApplicationServiceTests
{
    // Fresh substitutes for every test — no shared state between tests
    private readonly IApplicationRepository _appRepo;
    private readonly IJobListingRepository  _listingRepo;
    private readonly ApplicationService     _sut;

    public ApplicationServiceTests()
    {
        _appRepo     = Substitute.For<IApplicationRepository>();
        _listingRepo = Substitute.For<IJobListingRepository>();
        _sut         = new ApplicationService(_appRepo, _listingRepo);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Legal transitions — each row in InlineData is one test case
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ApplicationStatus.Submitted,   ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.UnderReview,  ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.UnderReview,  ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Shortlisted,  ApplicationStatus.Offered)]
    [InlineData(ApplicationStatus.Shortlisted,  ApplicationStatus.Rejected)]
    public async Task UpdateStatusAsync_WhenTransitionIsLegal_CallsUpdateStatusAsync(
        ApplicationStatus fromStatus, ApplicationStatus toStatus)
    {
        // Arrange — create an application in the From state
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var application = new Application
        {
            JobListingId = listingId,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow.AddDays(-1),
            Status       = fromStatus
        };

        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns(application);

        // Act — move to the To state
        await _sut.UpdateStatusAsync(listingId, applicantId, toStatus);

        // Assert — the transition is legal so UpdateStatusAsync must be called
        await _appRepo.Received(1).UpdateStatusAsync(
            application, toStatus, Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Illegal transitions — terminal states cannot move anywhere
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Offered,  ApplicationStatus.Submitted)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Offered,  ApplicationStatus.Shortlisted)]
    public async Task UpdateStatusAsync_WhenTransitionIsIllegal_ThrowsInvalidStatusTransitionException(
        ApplicationStatus fromStatus, ApplicationStatus toStatus)
    {
        // Arrange — create an application in the terminal From state
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var application = new Application
        {
            JobListingId = listingId,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow.AddDays(-1),
            Status       = fromStatus
        };

        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns(application);

        // Act
        var act = () => _sut.UpdateStatusAsync(listingId, applicantId, toStatus);

        // Assert — the transition is illegal so exception must be thrown
        await Assert.ThrowsAsync<InvalidStatusTransitionException>(act);

        // UpdateStatusAsync on the repository must NOT be called
        await _appRepo.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Application>(), Arg.Any<ApplicationStatus>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // Application not found
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateStatusAsync_WhenApplicationNotFound_ThrowsException()
    {
        // Arrange — repository returns null (application does not exist)
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns((Application?)null);

        // Act
        var act = () => _sut.UpdateStatusAsync(listingId, applicantId, ApplicationStatus.UnderReview);

        // Assert — exception thrown, repository update never called
        await Assert.ThrowsAnyAsync<Exception>(act);
        await _appRepo.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Application>(), Arg.Any<ApplicationStatus>(), Arg.Any<CancellationToken>());
    }
}
