using CareerHub.Api.DTOs;
using CareerHub.Api.Exceptions;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using CareerHub.Api.Services;
using NSubstitute;

namespace CareerHub.Api.Tests.Unit.Services;

// This class tests the JobListingService in isolation.
// "In isolation" means no real database, no real HTTP, no real anything.
// We use NSubstitute to create fake repositories that we control.
// Every test in this class follows the same pattern: Arrange, Act, Assert.

public class JobListingServiceTests
{
    // These are the fake repositories. They look like real ones but they
    // do nothing unless we tell them to. NSubstitute creates them.
    private readonly IJobListingRepository _listingRepo;
    private readonly ICompanyRepository    _companyRepo;

    // _sut stands for "System Under Test" — the actual class we are testing.
    // It receives the fake repositories instead of the real ones.
    private readonly JobListingService     _sut;

    // This constructor runs before EVERY test method.
    // That means each test gets fresh fakes with no leftover state
    // from a previous test. This is called test isolation.
    public JobListingServiceTests()
    {
        _listingRepo = Substitute.For<IJobListingRepository>();
        _companyRepo = Substitute.For<ICompanyRepository>();
        _sut         = new JobListingService(_listingRepo, _companyRepo);
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 1: Creating a job with SalaryMax less than SalaryMin must fail.
    //
    // Why this matters: If someone removes the salary check from the service,
    // a listing with SalaryMin=80000 and SalaryMax=50000 would be saved
    // to the database. Every response would show corrupt salary data.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]  // [Fact] tells xUnit this method is a test
    public async Task CreateAsync_WhenSalaryMaxLessThanSalaryMin_ThrowsInvalidListingException()
    {
        // ARRANGE — set up everything the test needs

        // We need a company id so the "does this company exist?" check passes.
        var companyId = Guid.NewGuid();

        // Create the request with a BAD salary range: min=80000, max=50000
        // The constructor parameters must match the exact order in CreateJobRequest.cs:
        // Title, CompanyId, Location, Description, Type, SalaryMin, SalaryMax, ClosingDate
        var request = new CreateJobRequest(
            "Developer",                              // Title
            companyId,                                // CompanyId
            "Bloemfontein",                           // Location
            "Build things for the enterprise platform", // Description (min 20 chars)
            JobType.FullTime,                         // Type
            80000,                                    // SalaryMin — deliberately higher than max
            50000,                                    // SalaryMax — deliberately lower than min
            DateTime.UtcNow.AddDays(30));             // ClosingDate — 30 days from now (valid)

        // Tell the fake company repo: "when someone asks if this company exists, say yes"
        // We want the company check to PASS so we can test the salary check specifically.
        _companyRepo.ExistsAsync(companyId).Returns(true);

        // ACT — call the method we are testing
        // We wrap it in a lambda so Assert.ThrowsAsync can catch the exception.
        var act = () => _sut.CreateAsync(request);

        // ASSERT — check what happened

        // The service must throw InvalidListingException because SalaryMax < SalaryMin
        await Assert.ThrowsAsync<InvalidListingException>(act);

        // The service must NOT have called AddAsync on the repository.
        // This proves the service stopped BEFORE saving to the database.
        // Arg.Any<T>() means "any value of this type" — we don't care about the specific arguments.
        await _listingRepo.DidNotReceive().AddAsync(Arg.Any<JobListing>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 2: Creating a job with a closing date in the past must fail.
    //
    // Why this matters: A listing that expired yesterday should never be
    // created. The service must catch this before saving.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_WhenClosingDateIsInThePast_ThrowsInvalidListingException()
    {
        // ARRANGE
        var companyId = Guid.NewGuid();

        // Everything is valid EXCEPT the closing date — it is yesterday
        var request = new CreateJobRequest(
            "Developer",
            companyId,
            "Bloemfontein",
            "Build things for the enterprise platform",
            JobType.FullTime,
            40000,                                    // valid salary range
            60000,
            DateTime.UtcNow.AddDays(-1));             // YESTERDAY — this is the problem

        // Company exists — that check passes
        _companyRepo.ExistsAsync(companyId).Returns(true);

        // ACT
        var act = () => _sut.CreateAsync(request);

        // ASSERT — must throw because closing date is in the past
        await Assert.ThrowsAsync<InvalidListingException>(act);

        // Must NOT have saved to the database
        await _listingRepo.DidNotReceive().AddAsync(Arg.Any<JobListing>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 3: Creating a valid job must actually save it to the database.
    //
    // Why this matters: This is the "happy path" test. If someone refactors
    // CreateAsync and accidentally breaks the save, this test catches it.
    // Without this test you could have a service that validates perfectly
    // but never actually saves anything.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_WhenValid_CallsAddAsyncExactlyOnce()
    {
        // ARRANGE — everything is valid
        var companyId = Guid.NewGuid();
        var request = new CreateJobRequest(
            "Developer",
            companyId,
            "Bloemfontein",
            "Build things for the enterprise platform",
            JobType.FullTime,
            40000,                                    // valid: min < max
            60000,
            DateTime.UtcNow.AddDays(30));             // valid: future date

        // Company exists
        _companyRepo.ExistsAsync(companyId).Returns(true);

        // After the listing is saved, CreateAsync calls GetDetailByIdAsync to return the response.
        // We tell the fake: "when someone asks for the detail, return this fake response."
        // Without this the service would get null back and crash.
        _listingRepo.GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new JobDetailResponse(
                Guid.NewGuid(),                       // Id
                "Developer",                          // Title
                "Build things for the enterprise platform", // Description
                "BitCube",                            // CompanyName
                "Bloemfontein",                       // Location
                JobType.FullTime,                     // Type
                40000,                                // SalaryMin
                60000,                                // SalaryMax
                "R40,000 – R60,000/month",            // SalaryDisplay
                DateTime.UtcNow,                      // PostedAt
                true,                                 // IsActive
                Enumerable.Empty<ApplicationSummary>(), // Applications (empty list)
                DateTime.UtcNow.AddDays(30),          // ClosingDate
                "Active"));                           // Status

        // ACT
        await _sut.CreateAsync(request);

        // ASSERT — AddAsync must have been called exactly once.
        // Received(1) means "this method was called exactly 1 time".
        // If it was called 0 times (save was skipped) the test fails.
        // If it was called 2 times (save was duplicated) the test also fails.
        await _listingRepo.Received(1).AddAsync(Arg.Any<JobListing>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 4: PATCHing only the salary with a value that exceeds SalaryMax must fail.
    //
    // Why this matters: PatchAsync has CONDITIONAL validation — it only checks
    // salary when a salary field is included in the request. If someone removes
    // that conditional guard, salary updates could skip validation entirely.
    // This test pins the correct behavior.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PatchAsync_WhenOnlySalaryMinChanged_AndExceedsSalaryMax_ThrowsInvalidListingException()
    {
        // ARRANGE
        var listingId = Guid.NewGuid();

        // The existing listing has SalaryMin=40000 and SalaryMax=60000
        // The JobListing constructor parameters:
        // Id, Title, Description, CompanyId, Location, Type, SalaryMin, SalaryMax,
        // PostedAt, IsActive, ClosingDate, Status
        var existing = new JobListing(
            listingId,
            "Developer",
            "Build things for the enterprise platform",
            Guid.NewGuid(),                           // CompanyId
            "Bloemfontein",
            JobType.FullTime,
            40000,                                    // existing SalaryMin
            60000,                                    // existing SalaryMax
            DateTime.UtcNow.AddDays(-5),              // PostedAt (5 days ago)
            true,                                     // IsActive
            DateTime.UtcNow.AddDays(30),              // ClosingDate (future)
            JobListingStatus.Active);

        // Tell the fake: "when someone asks for this listing, return the existing one"
        _listingRepo.GetEntityByIdAsync(listingId, Arg.Any<CancellationToken>()).Returns(existing);

        // The PATCH request only changes SalaryMin to 70000.
        // But 70000 > existing SalaryMax of 60000 — this is invalid.
        // All other fields are null meaning "don't change".
        var request = new UpdateJobListingRequest(
            Title:          null,                     // don't change
            Description:    null,                     // don't change
            Location:       null,                     // don't change
            EmploymentType: null,                     // don't change
            SalaryMin:      70000,                    // CHANGE — exceeds existing SalaryMax
            SalaryMax:      null,                     // don't change (stays at 60000)
            ClosingDate:    null);                    // don't change

        // ACT
        var act = () => _sut.PatchAsync(listingId, request);

        // ASSERT — service must throw because new SalaryMin (70000) > existing SalaryMax (60000)
        await Assert.ThrowsAsync<InvalidListingException>(act);

        // The repository's PatchAsync must NOT have been called.
        // The service caught the invalid salary BEFORE calling the repository.
        await _listingRepo.DidNotReceive().PatchAsync(
            Arg.Any<Guid>(), Arg.Any<UpdateJobListingRequest>(), Arg.Any<CancellationToken>());
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 5: PATCHing only the title must NOT trigger salary validation.
    //
    // Why this matters: This is the opposite of Test 4. Together they prove
    // the conditional logic works both ways:
    //   Salary field present  → validation runs (Test 4)
    //   No salary field       → validation does NOT run (this test)
    //
    // If someone breaks the conditional guard, one of these two tests fails.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PatchAsync_WhenOnlyTitleChanged_DoesNotThrowSalaryException()
    {
        // ARRANGE
        var listingId = Guid.NewGuid();
        var existing = new JobListing(
            listingId,
            "Old Title",
            "Build things for the enterprise platform",
            Guid.NewGuid(),
            "Bloemfontein",
            JobType.FullTime,
            40000,
            60000,
            DateTime.UtcNow.AddDays(-5),
            true,
            DateTime.UtcNow.AddDays(30),
            JobListingStatus.Active);

        _listingRepo.GetEntityByIdAsync(listingId, Arg.Any<CancellationToken>()).Returns(existing);

        // Tell the fake: "when PatchAsync is called, return a successful response"
        // This simulates the repository applying the title change and returning the result.
        _listingRepo.PatchAsync(listingId, Arg.Any<UpdateJobListingRequest>(), Arg.Any<CancellationToken>())
            .Returns(new JobResponse(
                listingId,
                "New Title",                          // title was changed
                "Build things for the enterprise platform",
                "BitCube",
                "Bloemfontein",
                JobType.FullTime,
                40000,                                // salary unchanged
                60000,                                // salary unchanged
                "R40,000 – R60,000/month",
                DateTime.UtcNow,
                true,
                0,                                    // application count
                DateTime.UtcNow.AddDays(30),
                "Active"));

        // Only Title is set — all salary fields are null meaning "don't change"
        var request = new UpdateJobListingRequest(
            Title:          "New Title",              // CHANGE — only this field
            Description:    null,
            Location:       null,
            EmploymentType: null,
            SalaryMin:      null,                     // don't change
            SalaryMax:      null,                     // don't change
            ClosingDate:    null);

        // ACT — this should NOT throw any exception
        var result = await _sut.PatchAsync(listingId, request);

        // ASSERT

        // The repository's PatchAsync MUST have been called — the update went through.
        // Received(1) means it was called exactly once.
        await _listingRepo.Received(1).PatchAsync(
            listingId, Arg.Any<UpdateJobListingRequest>(), Arg.Any<CancellationToken>());

        // The returned title must be the new one
        Assert.Equal("New Title", result.Title);
    }

    // ══════════════════════════════════════════════════════════════════════
    // TEST 6: PATCHing a listing that does not exist must throw 404.
    //
    // Why this matters: If the service tries to patch a non-existent listing
    // without checking first, it would crash with a NullReferenceException
    // instead of a clean 404 Not Found. This test ensures the service
    // checks existence before doing anything.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PatchAsync_WhenListingNotFound_ThrowsJobNotFoundException()
    {
        // ARRANGE
        var listingId = Guid.NewGuid();

        // Tell the fake: "when someone asks for this listing, return null"
        // null means the listing does not exist in the database.
        _listingRepo.GetEntityByIdAsync(listingId, Arg.Any<CancellationToken>())
            .Returns((JobListing?)null);

        var request = new UpdateJobListingRequest(
            Title:          "New Title",
            Description:    null,
            Location:       null,
            EmploymentType: null,
            SalaryMin:      null,
            SalaryMax:      null,
            ClosingDate:    null);

        // ACT
        var act = () => _sut.PatchAsync(listingId, request);

        // ASSERT — service must throw JobNotFoundException
        await Assert.ThrowsAsync<JobNotFoundException>(act);

        // The repository's PatchAsync must NOT have been called.
        // There is nothing to patch — the listing does not exist.
        await _listingRepo.DidNotReceive().PatchAsync(
            Arg.Any<Guid>(), Arg.Any<UpdateJobListingRequest>(), Arg.Any<CancellationToken>());
    }
}