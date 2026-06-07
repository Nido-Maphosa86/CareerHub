using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Data;

// WHAT CHANGED FROM 2.3:
// - Check constraints added to job_listings and applications (Part 2)
// - Composite indexes added for active listing and company-scoped queries (Part 3)
// - GIN index on SearchVector for full-text search (Part 3 + Part 5)
// - SearchVector configured as a computed stored tsvector column (Part 5)

public class CareerHubDbContext(DbContextOptions<CareerHubDbContext> options)
    : DbContext(options)
{
    public DbSet<JobListing>  JobListings  => Set<JobListing>();
    public DbSet<Company>     Companies    => Set<Company>();
    public DbSet<Applicant>   Applicants   => Set<Applicant>();
    public DbSet<Application> Applications => Set<Application>();

    public static readonly Guid Applicant1Id = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid Applicant2Id = Guid.Parse("a0000000-0000-0000-0000-000000000002");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ══════════════════════════════════════════════════════════════════
        // COMPANY
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Website).HasMaxLength(500);
            entity.Property(c => c.Industry).HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // ══════════════════════════════════════════════════════════════════
        // JOB LISTING
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<JobListing>(entity =>
        {
            // ── Table + CHECK CONSTRAINTS (Part 2) ────────────────────────
            // Constraints enforce business rules at the database level.
            // Even if the service layer is bypassed (direct psql INSERT, batch script,
            // another service writing directly), the database will still reject bad data.
            entity.ToTable("job_listings", t =>
            {
                // SalaryMin must be positive when provided.
                // Without this: a listing could be inserted with SalaryMin = -50,000.
                t.HasCheckConstraint(
                    "ck_job_listings_salarymin_positive",
                    "\"SalaryMin\" IS NULL OR \"SalaryMin\" > 0");

                // SalaryMax must be greater than SalaryMin when both are provided.
                // The IS NULL guards handle the case where either value is null —
                // a null salary range is permitted (not specified), but a range where
                // max < min is corrupt data.
                t.HasCheckConstraint(
                    "ck_job_listings_salarymax_gt_min",
                    "\"SalaryMax\" IS NULL OR \"SalaryMin\" IS NULL OR \"SalaryMax\" > \"SalaryMin\"");

                // ClosingDate must be after PostedAt.
                // Without this: a listing could be created already expired.
                // The service enforces ClosingDate > UtcNow, but the database
                // ensures ClosingDate > PostedAt as a minimum sanity check.
                t.HasCheckConstraint(
                    "ck_job_listings_closingdate_after_postedat",
                    "\"ClosingDate\" > \"PostedAt\"");
            });

            entity.HasKey(j => j.Id);
            entity.Property(j => j.Id).ValueGeneratedNever();
            entity.Property(j => j.Title).IsRequired().HasMaxLength(120);
            entity.Property(j => j.Description).IsRequired().HasMaxLength(2000);
            entity.Property(j => j.Location).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(j => j.SalaryMin).HasPrecision(18, 2);
            entity.Property(j => j.SalaryMax).HasPrecision(18, 2);

            // Status stored as string — "Active" / "Closed"
            entity.Property(j => j.Status)
                  .HasConversion<string>()
                  .IsRequired()
                  .HasMaxLength(20);

            // ── FULL-TEXT SEARCH: computed stored tsvector column (Part 5) ──
            // The database generates this column from Title and Description
            // using the English stemming and stop-word configuration.
            // 'stored: true' means it is computed once at write time, not on every read.
            // This is what allows the GIN index to work — an index on a volatile
            // expression cannot be used efficiently.
            entity.Property(j => j.SearchVector)
                  .HasColumnType("tsvector")
                  .HasComputedColumnSql(
                      "to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"Description\", ''))",
                      stored: true);

            // ── INDEXES (Part 3) ──────────────────────────────────────────

            // Active listing query — most frequent query in the system.
            // Called on every page load of the job board.
            // Status first: narrows to Active rows immediately (high selectivity on small set).
            // ClosingDate second: further filters within Active rows.
            // Column order: Status first because it eliminates Closed rows before scanning dates.
            entity.HasIndex(j => new { j.Status, j.ClosingDate })
                  .HasDatabaseName("ix_job_listings_status_closingdate");

            // Company-scoped listing query — employer views their own postings.
            // CompanyId first: one company's listings is a very small subset.
            // Status second: filters Active/Closed within that company.
            entity.HasIndex(j => new { j.CompanyId, j.Status })
                  .HasDatabaseName("ix_job_listings_companyid_status");

            // GIN index on the stored tsvector column — enables fast full-text search.
            // B-tree cannot index tsvector — GIN is the correct index type for this.
            entity.HasIndex(j => j.SearchVector)
                  .HasMethod("GIN")
                  .HasDatabaseName("ix_job_listings_searchvector");

            entity.HasIndex(j => new { j.Title, j.CompanyId })
                  .IsUnique()
                  .HasDatabaseName("ix_job_listings_title_companyid");

            // ── RELATIONSHIPS ─────────────────────────────────────────────
            entity.HasOne(j => j.Company)
                  .WithMany(c => c.JobListings)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ══════════════════════════════════════════════════════════════════
        // APPLICANT
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Applicant>(entity =>
        {
            entity.ToTable("applicants");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Email).IsRequired().HasMaxLength(200);
            entity.Property(a => a.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(a => a.Email).IsUnique();
            entity.HasIndex(a => a.Username).IsUnique();

            entity.HasData(
                new Applicant { Id = Applicant1Id, Name = "Alice Smith", Email = "alice@example.com", Username = "applicant1" },
                new Applicant { Id = Applicant2Id, Name = "Bob Jones",   Email = "bob@example.com",   Username = "applicant2" }
            );
        });

        // ══════════════════════════════════════════════════════════════════
        // APPLICATION (join entity)
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Application>(entity =>
        {
            // ── Table + CHECK CONSTRAINT (Part 2) ─────────────────────────
            // SubmittedAt must not be in the future.
            // Without this: a script could insert applications backdated to months ago,
            // making it appear the applicant applied before the listing existed.
            entity.ToTable("applications", t =>
            {
                t.HasCheckConstraint(
                    "ck_applications_submittedAt_not_future",
                    "\"SubmittedAt\" <= NOW()");
            });

            entity.HasKey(a => new { a.ApplicantId, a.JobListingId });
            entity.Property(a => a.SubmittedAt).IsRequired();
            entity.Property(a => a.Status)
                  .HasConversion<string>()
                  .IsRequired()
                  .HasMaxLength(20);

            // ── INDEXES (Part 3) ──────────────────────────────────────────

            // HasAlreadyApplied check — called on every application submission.
            // JobListingId first: the most common filter — "has anyone applied to this job?"
            entity.HasIndex(a => new { a.JobListingId, a.ApplicantId })
                  .HasDatabaseName("ix_applications_joblistingid_applicantid");

            // Employer dashboard — all applications for a listing.
            entity.HasIndex(a => a.JobListingId)
                  .HasDatabaseName("ix_applications_joblistingid");

            entity.HasOne(a => a.JobListing)
                  .WithMany(j => j.Applications)
                  .HasForeignKey(a => a.JobListingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Applicant)
                  .WithMany(ap => ap.Applications)
                  .HasForeignKey(a => a.ApplicantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
