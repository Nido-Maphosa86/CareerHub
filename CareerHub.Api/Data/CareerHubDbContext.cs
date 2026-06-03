using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Data;

public class CareerHubDbContext(DbContextOptions<CareerHubDbContext> options)
    : DbContext(options)
{
    // ── DbSets — one per table ────────────────────────────────────────────
    public DbSet<JobListing>  JobListings  => Set<JobListing>();
    public DbSet<Company>     Companies    => Set<Company>();
    public DbSet<Applicant>   Applicants   => Set<Applicant>();
    public DbSet<Application> Applications => Set<Application>();

    // ── Seeded applicant IDs (fixed so AuthController can reference them) ──
    // These GUIDs are stable — they match what AuthController puts in the JWT.
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

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(c => c.Website).HasMaxLength(500);
            entity.Property(c => c.Industry).HasMaxLength(100);

            // Two companies with the same name would be confusing —
            // enforce uniqueness at the database level
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // ══════════════════════════════════════════════════════════════════
        // JOB LISTING
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<JobListing>(entity =>
        {
            entity.ToTable("job_listings");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Id).ValueGeneratedNever();

            entity.Property(j => j.Title).IsRequired().HasMaxLength(120);
            entity.Property(j => j.Description).IsRequired().HasMaxLength(2000);
            entity.Property(j => j.Location).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Type).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(j => j.SalaryMin).HasPrecision(18, 2);
            entity.Property(j => j.SalaryMax).HasPrecision(18, 2);

            entity.HasIndex(j => new { j.Title, j.CompanyId })
                  .IsUnique()
                  .HasDatabaseName("ix_job_listings_title_companyid");

            // ── Company → JobListing relationship ─────────────────────────
            // DELETE BEHAVIOUR: Restrict
            // A company CANNOT be deleted while it still has job listings.
            // This forces explicit cleanup — an employer must remove all
            // listings before the company record can be removed.
            // Cascade would silently wipe listings, which is dangerous on a job board.
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

            // ── Seed data — matches AuthController hardcoded credentials ──
            entity.HasData(
                new Applicant
                {
                    Id       = Applicant1Id,
                    Name     = "Alice Smith",
                    Email    = "alice@example.com",
                    Username = "applicant1"
                },
                new Applicant
                {
                    Id       = Applicant2Id,
                    Name     = "Bob Jones",
                    Email    = "bob@example.com",
                    Username = "applicant2"
                }
            );
        });

        // ══════════════════════════════════════════════════════════════════
        // APPLICATION (join entity)
        // ══════════════════════════════════════════════════════════════════
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("applications");

            // Composite primary key — one applicant can only apply once per listing.
            // This is the natural uniqueness rule for an application.
            entity.HasKey(a => new { a.ApplicantId, a.JobListingId });

            entity.Property(a => a.SubmittedAt).IsRequired();

            // Store status as a string in the database
            entity.Property(a => a.Status)
                  .HasConversion<string>()
                  .IsRequired()
                  .HasMaxLength(20);

            // ── Application → JobListing ───────────────────────────────────
            // Cascade: deleting a job listing removes all its applications.
            // Justified because an application for a non-existent listing is meaningless.
            entity.HasOne(a => a.JobListing)
                  .WithMany(j => j.Applications)
                  .HasForeignKey(a => a.JobListingId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ── Application → Applicant ────────────────────────────────────
            // Cascade: deleting an applicant removes all their applications.
            entity.HasOne(a => a.Applicant)
                  .WithMany(ap => ap.Applications)
                  .HasForeignKey(a => a.ApplicantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
