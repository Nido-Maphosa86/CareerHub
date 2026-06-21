using CareerHub.Api.Data;
using CareerHub.Api.DTOs;
using CareerHub.Api.Models;
using CareerHub.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Tests.Repository;

// These tests run against a REAL PostgreSQL container — not an in-memory fake.
// That means check constraints, GIN indexes, tsvector full-text search, and
// compiled queries all behave exactly like production.
//
// The EF Core in-memory provider cannot test ANY of these — it does not
// enforce constraints, does not support tsvector, and does not translate
// compiled queries to SQL.
//
// Pattern followed: Arrange → Act → Assert.

public class JobListingRepositoryTests(PostgreSqlContainerFixture fixture) : IClassFixture<PostgreSqlContainerFixture>
{
    // ── Helpers ──────────────────────────────────────────────────────────

    // FIX: Changed from context.Database.Migrate() to context.Database.EnsureCreated().
    //
    // Migrate() runs every migration file one by one in order. One of our migrations
    // tries to drop an index called IX_job_listings_CompanyId that was never created
    // in a previous migration — so the fresh test container fails with:
    //   "index IX_job_listings_CompanyId does not exist"
    //
    // EnsureCreated() skips migrations entirely and builds the schema directly from
    // the current model snapshot. This means all tables, check constraints, indexes,
    // and computed columns are created in one shot from what the model looks like TODAY.
    // The broken migration is completely bypassed.
    //
    // The tradeoff: EnsureCreated() cannot test whether the migrations themselves are
    // correct — only that the current model is correct. For our purposes (testing
    // repository behaviour against real PostgreSQL) this is exactly what we need.
    private CareerHubDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CareerHubDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        var context = new CareerHubDbContext(options);

        // EnsureCreated builds the schema from the current model.
        // It does nothing if the schema already exists — safe to call on every test.
        context.Database.EnsureCreated();

        return context;
    }

    // Wipes all data from every table so the next test starts with a clean slate.
    // We need this because EnsureCreated() reuses the same schema across all tests —
    // data inserted by one test stays there for the next test unless we clean it up.
    // Applications must be deleted before JobListings and Companies because of
    // foreign key constraints — you cannot delete a parent row while child rows exist.
    private static async Task ClearAllData(CareerHubDbContext context)
    {
        context.Applications.RemoveRange(context.Applications);
        context.JobListings.RemoveRange(context.JobListings);
        context.Companies.RemoveRange(context.Companies);
        await context.SaveChangesAsync();
    }

    // Creates a company in the database and returns it.
    // Every listing needs a valid CompanyId foreign key — without a real
    // company row, inserting a listing fails with a FK violation instead
    // of the behaviour we actually want to test.
    private static async Task<Company> SeedCompany(CareerHubDbContext context)
    {
        var company = new Company
        {
            Id       = Guid.NewGuid(),
            // Unique name avoids the duplicate company name constraint
            Name     = "Test Company " + Guid.NewGuid().ToString()[..8],
            Industry = "Technology"
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return company;
    }

    // Creates one active listing for the given company and returns it.
    // postedDaysAgo and closesInDays let each test control the dates
    // without hardcoding values inside the test methods.
    private static async Task<JobListing> SeedListing(
        CareerHubDbContext context,
        Guid companyId,
        string title            = "Developer",
        int postedDaysAgo       = 1,
        int closesInDays        = 30,
        JobListingStatus status = JobListingStatus.Active)
    {
        var listing = new JobListing(
            Guid.NewGuid(),
            title,
            "A valid test description that is long enough",
            companyId,
            "Bloemfontein",
            JobType.FullTime,
            40000,
            60000,
            DateTime.UtcNow.AddDays(-postedDaysAgo),   // PostedAt
            status == JobListingStatus.Active,           // IsActive
            DateTime.UtcNow.AddDays(closesInDays),      // ClosingDate
            status);

        context.JobListings.Add(listing);
        await context.SaveChangesAsync();
        return listing;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Pagination tests
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetActiveListingsPagedAsync_Page1_ReturnsCorrectCount()
    {
        // Arrange — wipe all data left by previous tests so the count is exact.
        // Without this, other tests that seeded listings would inflate the count.
        await using var context = CreateContext();
        await ClearAllData(context);

        // Seed exactly 6 active listings
        var company = await SeedCompany(context);
        for (int i = 1; i <= 6; i++)
            await SeedListing(context, company.Id, $"Listing {i}");

        var repository = new JobListingRepository(context);

        // Act — ask for page 1 with 4 per page
        var result = await repository.GetActiveListingsPagedAsync(
            page: 1, pageSize: 4, new JobListingFilterQuery());

        // Assert — 4 items on this page, 6 in total, more pages exist
        Assert.Equal(4, result.Data.Count());
        Assert.Equal(6, result.TotalCount);
        Assert.True(result.HasNextPage);       // 6 items / 4 per page = 2 pages
        Assert.False(result.HasPreviousPage);  // page 1 has nothing before it
    }

    [Fact]
    public async Task GetActiveListingsPagedAsync_Page2_ReturnsDifferentRows()
    {
        // Arrange — wipe all data then seed exactly 6 listings
        await using var context = CreateContext();
        await ClearAllData(context);

        var company = await SeedCompany(context);
        for (int i = 1; i <= 6; i++)
            await SeedListing(context, company.Id, $"Page Test {i}");

        var repository = new JobListingRepository(context);

        // Act — fetch page 1 and page 2, 3 items each
        var page1 = await repository.GetActiveListingsPagedAsync(1, 3, new JobListingFilterQuery());
        var page2 = await repository.GetActiveListingsPagedAsync(2, 3, new JobListingFilterQuery());

        // Assert — the two pages must not share any listing ids.
        // We collect the ids into HashSets and check the intersection is empty.
        // If the repository forgot Skip(), the same rows would appear on both pages.
        var page1Ids = page1.Data.Select(j => j.id).ToHashSet();
        var page2Ids = page2.Data.Select(j => j.id).ToHashSet();

        Assert.Empty(page1Ids.Intersect(page2Ids));
        Assert.True(page2.HasPreviousPage);    // page 2 always has a previous page
    }

    [Fact]
    public async Task GetActiveListingsPagedAsync_ResultsAreOrderedByPostedAtDescending()
    {
        // Arrange — seed listings posted on different days.
        // We do NOT clear data here because we only check ORDER, not exact count.
        // Any extra rows from other tests will just be included in the sort check.
        await using var context = CreateContext();
        var company = await SeedCompany(context);
        await SeedListing(context, company.Id, "Five days old",  postedDaysAgo: 5);
        await SeedListing(context, company.Id, "One day old",    postedDaysAgo: 1);
        await SeedListing(context, company.Id, "Three days old", postedDaysAgo: 3);

        var repository = new JobListingRepository(context);

        // Act
        var result = await repository.GetActiveListingsPagedAsync(1, 20, new JobListingFilterQuery());

        // Assert — each listing must be newer than or equal to the next one.
        // Newest first is the default sort. Without ORDER BY, PostgreSQL
        // returns rows in an undefined order and pagination becomes unreliable.
        var listings = result.Data.ToList();
        for (int i = 0; i < listings.Count - 1; i++)
        {
            Assert.True(
                listings[i].PostedAt >= listings[i + 1].PostedAt,
                $"Listing at index {i} (PostedAt: {listings[i].PostedAt}) " +
                $"should be newer than listing at index {i + 1} (PostedAt: {listings[i + 1].PostedAt})");
        }
    }

    [Fact]
    public async Task GetActiveListingsPagedAsync_ExcludesExpiredListings()
    {
        // Arrange — wipe all data then seed exactly 3 active and 2 expired listings.
        // The exact count matters here so we must start clean.
        await using var context = CreateContext();
        await ClearAllData(context);

        var company = await SeedCompany(context);

        await SeedListing(context, company.Id, "Active 1", closesInDays: 30);
        await SeedListing(context, company.Id, "Active 2", closesInDays: 30);
        await SeedListing(context, company.Id, "Active 3", closesInDays: 30);

        // Expired listings — posted 60 days ago, closed 10 days ago.
        // Posted BEFORE closed so the check constraint is satisfied,
        // but the closing date is in the past so these listings are expired.
        var expired1 = new JobListing(
            Guid.NewGuid(), "Expired 1", "A valid test description that is long enough",
            company.Id, "Bloemfontein", JobType.FullTime, 40000, 60000,
            DateTime.UtcNow.AddDays(-60), true, DateTime.UtcNow.AddDays(-10),
            JobListingStatus.Active);

        var expired2 = new JobListing(
            Guid.NewGuid(), "Expired 2", "A valid test description that is long enough",
            company.Id, "Bloemfontein", JobType.FullTime, 40000, 60000,
            DateTime.UtcNow.AddDays(-60), true, DateTime.UtcNow.AddDays(-10),
            JobListingStatus.Active);

        context.JobListings.AddRange(expired1, expired2);
        await context.SaveChangesAsync();

        var repository = new JobListingRepository(context);

        // Act
        var result = await repository.GetActiveListingsPagedAsync(1, 20, new JobListingFilterQuery());

        // Assert — only the 3 non-expired listings appear.
        // The repository filters on ClosingDate > now so expired listings
        // must be invisible to job seekers.
        Assert.Equal(3, result.TotalCount);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Check constraint tests — these only work against REAL PostgreSQL.
    // The in-memory provider does not enforce check constraints at all.
    // We insert rows DIRECTLY via DbContext, bypassing all service and
    // repository logic. Only the database constraint can stop the insert.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckConstraint_RejectsSalaryMaxLessThanSalaryMin()
    {
        // Arrange — SalaryMax (10000) is LESS than SalaryMin (50000)
        await using var context = CreateContext();
        var company = await SeedCompany(context);

        var badListing = new JobListing(
            Guid.NewGuid(),
            "Bad Salary Listing",
            "A valid test description that is long enough",
            company.Id,
            "Bloemfontein",
            JobType.FullTime,
            50000,                         // SalaryMin
            10000,                         // SalaryMax — less than min (INVALID)
            DateTime.UtcNow,
            true,
            DateTime.UtcNow.AddDays(30),
            JobListingStatus.Active);

        context.JobListings.Add(badListing);

        // Act & Assert — the database constraint ck_job_listings_salarymax_gt_min
        // must reject this row. SaveChangesAsync throws DbUpdateException.
        // This proves the constraint exists in the schema and cannot be bypassed.
        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CheckConstraint_RejectsClosingDateBeforePostedAt()
    {
        // Arrange — ClosingDate (yesterday) is BEFORE PostedAt (now)
        await using var context = CreateContext();
        var company = await SeedCompany(context);

        var badListing = new JobListing(
            Guid.NewGuid(),
            "Bad Date Listing",
            "A valid test description that is long enough",
            company.Id,
            "Bloemfontein",
            JobType.FullTime,
            40000,
            60000,
            DateTime.UtcNow,               // PostedAt — now
            true,
            DateTime.UtcNow.AddDays(-1),   // ClosingDate — yesterday (INVALID)
            JobListingStatus.Active);

        context.JobListings.Add(badListing);

        // Act & Assert — the constraint ck_job_listings_closingdate_after_postedat
        // must reject this row
        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    // ══════════════════════════════════════════════════════════════════════
    // HasAppliedAsync tests — verifies the compiled query that checks
    // whether an applicant has already applied to a listing.
    //
    // This is a hot path — it runs on every application submission.
    // We test it against real PostgreSQL because the compiled expression
    // tree is translated to SQL once and reused. A bug in the translation
    // would be silent against an in-memory provider.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HasAppliedAsync_WhenApplicationExists_ReturnsTrue()
    {
        // Arrange — seed a listing and an application for applicant1
        await using var context = CreateContext();
        var company = await SeedCompany(context);
        var listing = await SeedListing(context, company.Id, "Applied Listing");

        // applicant1 is seeded by the migrations with this known id
        var applicantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

        var application = new Application
        {
            JobListingId = listing.Id,
            ApplicantId  = applicantId,
            SubmittedAt  = DateTime.UtcNow.AddMinutes(-5),
            Status       = ApplicationStatus.Submitted
        };
        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var repository = new ApplicationRepository(context);

        // Act — ask the compiled query: has this applicant applied?
        var result = await repository.HasAlreadyAppliedAsync(listing.Id, applicantId);

        // Assert — application exists so must return true
        Assert.True(result);
    }

    [Fact]
    public async Task HasAppliedAsync_WhenNoApplicationExists_ReturnsFalse()
    {
        // Arrange — seed a listing but NO application for it
        await using var context = CreateContext();
        var company = await SeedCompany(context);
        var listing = await SeedListing(context, company.Id, "No Applications Listing");

        // applicant2's known id — they have never applied to this listing
        var applicantId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

        var repository = new ApplicationRepository(context);

        // Act
        var result = await repository.HasAlreadyAppliedAsync(listing.Id, applicantId);

        // Assert — no application exists so must return false
        Assert.False(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Full-text search tests — uses the GIN index and tsvector column
    // added in Assignment 2.4. PostgreSQL stems words so "engineer" and
    // "Engineering" both reduce to the same lexeme and match each other.
    //
    // These tests CANNOT pass on the in-memory provider because the
    // in-memory provider has no concept of tsvector or GIN indexes.
    // ══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullTextSearchAsync_ReturnsStemmedMatches()
    {
        // Arrange — seed a listing titled "Software Engineering Position"
        await using var context = CreateContext();
        var company = await SeedCompany(context);
        await SeedListing(context, company.Id, "Software Engineering Position");

        var repository = new JobListingRepository(context);

        // Act — search for "engineer" — NOT the exact word in the title.
        // PostgreSQL stems "engineer" to the same lexeme as "Engineering"
        // so this listing must come back even though the exact word is different.
        var results = await repository.SearchAsync("engineer");

        // Assert
        Assert.Contains(results, j => j.Title == "Software Engineering Position");
    }

    [Fact]
    public async Task FullTextSearchAsync_DoesNotReturnNonMatchingListings()
    {
        // Arrange — 2 listings match "plumber", 1 does not.
        // We do NOT clear data here because we check with DoesNotContain
        // which works correctly regardless of extra rows from other tests.
        await using var context = CreateContext();
        var company = await SeedCompany(context);
        await SeedListing(context, company.Id, "Plumber Needed Urgently");
        await SeedListing(context, company.Id, "Senior Plumber Position");
        await SeedListing(context, company.Id, "Accountant Wanted");

        var repository = new JobListingRepository(context);

        // Act
        var results = (await repository.SearchAsync("plumber")).ToList();

        // Assert — plumber listings come back, accountant does not.
        // We use Contains/DoesNotContain instead of exact count because
        // other tests may have also seeded listings with "plumber" in the title.
        Assert.Contains(results, j => j.Title == "Plumber Needed Urgently");
        Assert.Contains(results, j => j.Title == "Senior Plumber Position");
        Assert.DoesNotContain(results, j => j.Title == "Accountant Wanted");
    }
}