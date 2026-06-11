using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using NSubstitute;

namespace CareerHub.Api.Tests.Unit.Services;

// This class tests the ApplicationService in isolation.
// It focuses on the status transition workflow — the rules that define
// which status moves are allowed and which are blocked.
//
// The status workflow looks like this:
//   Submitted → UnderReview → Shortlisted → Offered
//                            → Rejected
//              Shortlisted  → Rejected
//
// Offered and Rejected are terminal states — no further moves allowed.

public class ApplicationServiceTests
{
    // Fake application repository — we control what it returns
    private readonly IApplicationRepository _appRepo;

    // Fake job listing repository — needed because ApplicationService depends on it
    private readonly IJobListingRepository  _listingRepo;

    // The actual class we are testing
    private readonly ApplicationService     _sut;

    // This constructor runs before EVERY test method.
    // Each test gets fresh fakes with no leftover state from previous tests.
    public ApplicationServiceTests()
    {
        _appRepo     = Substitute.For<IApplicationRepository>();
        _listingRepo = Substitute.For<IJobListingRepository>();
        _sut         = new ApplicationService(_appRepo, _listingRepo);
    }

    // ══════════════════════════════════════════════════════════════════════
    // LEGAL TRANSITIONS — all five allowed status moves
    //
    // [Theory] means this test method runs multiple times with different data.
    // Each [InlineData] is one test case — one run with specific From and To values.
    // This is better than writing 5 separate test methods that do the same thing.
    //
    // If you add a new legal transition later (like Offered → Accepted),
    // you just add one more [InlineData] line. No new method needed.
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ApplicationStatus.Submitted,   ApplicationStatus.UnderReview)]   // Case 1: Submitted → UnderReview
    [InlineData(ApplicationStatus.UnderReview,  ApplicationStatus.Shortlisted)]  // Case 2: UnderReview → Shortlisted
    [InlineData(ApplicationStatus.UnderReview,  ApplicationStatus.Rejected)]     // Case 3: UnderReview → Rejected
    [InlineData(ApplicationStatus.Shortlisted,  ApplicationStatus.Offered)]      // Case 4: Shortlisted → Offered
    [InlineData(ApplicationStatus.Shortlisted,  ApplicationStatus.Rejected)]     // Case 5: Shortlisted → Rejected
    public async Task UpdateStatusAsync_WhenTransitionIsLegal_CallsUpdateStatusAsync(
        ApplicationStatus fromStatus,    // the current status of the application
        ApplicationStatus toStatus)      // the status we want to move to
    {
        // ARRANGE — create a fake application in the From state
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        // This fake application is sitting in the fromStatus state.
        // For example, if fromStatus is Submitted, this application is currently Submitted.
        var application = new Application
        {
            JobListingId = listingId,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow.AddDays(-1),
            Status       = fromStatus    // the current status BEFORE the move
        };

        // Tell the fake: "when someone asks for this application, return it"
        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns(application);

        // ACT — try to move the application to the toStatus
        // For example: move from Submitted to UnderReview
        await _sut.UpdateStatusAsync(listingId, applicantId, toStatus);

        // ASSERT — the transition is legal, so UpdateStatusAsync on the repository
        // MUST have been called exactly once. This means the status was actually saved.
        // Received(1) = called exactly 1 time.
        await _appRepo.Received(1).UpdateStatusAsync(
            application,                  // the same application object
            toStatus,                     // the new status
            Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // ILLEGAL TRANSITIONS — terminal states cannot move anywhere
    //
    // Rejected and Offered are dead ends. Once an application reaches
    // either of these states, no further moves are allowed.
    //
    // If someone accidentally removes the transition guard from the service,
    // all four of these tests fail immediately. The workflow is pinned.
    // ══════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Submitted)]    // Case 1: Rejected → Submitted (blocked)
    [InlineData(ApplicationStatus.Offered,  ApplicationStatus.Submitted)]    // Case 2: Offered → Submitted (blocked)
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.UnderReview)]  // Case 3: Rejected → UnderReview (blocked)
    [InlineData(ApplicationStatus.Offered,  ApplicationStatus.Shortlisted)]  // Case 4: Offered → Shortlisted (blocked)
    public async Task UpdateStatusAsync_WhenTransitionIsIllegal_ThrowsInvalidStatusTransitionException(
        ApplicationStatus fromStatus,    // the terminal state the application is stuck in
        ApplicationStatus toStatus)      // the state someone is trying to move it to
    {
        // ARRANGE — create a fake application in the terminal From state
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        // This application is in a terminal state — Rejected or Offered.
        // It should not be allowed to move anywhere.
        var application = new Application
        {
            JobListingId = listingId,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow.AddDays(-1),
            Status       = fromStatus    // terminal state
        };

        // Tell the fake: "when someone asks for this application, return it"
        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns(application);

        // ACT — try the illegal move
        // For example: try to move a Rejected application back to Submitted
        var act = () => _sut.UpdateStatusAsync(listingId, applicantId, toStatus);

        // ASSERT — the service must throw InvalidStatusTransitionException
        // because the move is not allowed
        await Assert.ThrowsAsync<InvalidStatusTransitionException>(act);

        // UpdateStatusAsync on the repository must NOT have been called.
        // DidNotReceive() means "this method was never called".
        // The service blocked the move BEFORE saving to the database.
        await _appRepo.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Application>(),
            Arg.Any<ApplicationStatus>(),
            Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // APPLICATION NOT FOUND
    //
    // If the application does not exist in the database, the service must
    // throw an exception. It should not try to update something that
    // does not exist — that would cause a NullReferenceException crash
    // instead of a clean error message.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]  // [Fact] = one test case, not multiple like [Theory]
    public async Task UpdateStatusAsync_WhenApplicationNotFound_ThrowsException()
    {
        // ARRANGE
        var listingId   = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        // Tell the fake: "when someone asks for this application, return null"
        // null means the application does not exist in the database.
        _appRepo.GetEntityAsync(listingId, applicantId, Arg.Any<CancellationToken>())
            .Returns((Application?)null);

        // ACT — try to update the status of a non-existent application
        var act = () => _sut.UpdateStatusAsync(listingId, applicantId, ApplicationStatus.UnderReview);

        // ASSERT — some exception must be thrown (the exact type depends on your service)
        // ThrowsAnyAsync means "any exception type is acceptable"
        await Assert.ThrowsAnyAsync<Exception>(act);

        // The repository's UpdateStatusAsync must NOT have been called.
        // There is nothing to update — the application does not exist.
        await _appRepo.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Application>(),
            Arg.Any<ApplicationStatus>(),
            Arg.Any<CancellationToken>());
    }
}