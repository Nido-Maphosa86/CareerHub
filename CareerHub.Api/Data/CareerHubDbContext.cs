using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Data;

// The DbContext is the unit of work for EF Core.
// It owns the database connection, the change tracker,
// and provides access to every table through DbSet<T> properties.
// Registered as Scoped in Program.cs — one instance per HTTP request.

public class CareerHubDbContext(DbContextOptions<CareerHubDbContext> options)
    : DbContext(options)
{
    // DbSet<T> represents the job_listings table.
    // Every LINQ query you write against this property
    // gets translated into SQL by EF Core at runtime.
    public DbSet<JobListing> JobListings => Set<JobListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobListing>(entity =>
        {
            // ── Table name ────────────────────────────────────────────
            // PostgreSQL convention uses lowercase snake_case table names
            entity.ToTable("job_listings");

            // ── Primary key ───────────────────────────────────────────
            entity.HasKey(j => j.Id);

            // ValueGeneratedNever — our application supplies the Guid before saving.
            // We know the ID before the database round-trip completes,
            // which means we can build the 201 Location header immediately.
            entity.Property(j => j.Id)
                  .ValueGeneratedNever();

            // ── String constraints ────────────────────────────────────
            // Enforced at the database level — defence in depth.
            // The same rules exist in CreateJobRequest via Data Annotations.
            // Two layers of protection: application code and database.

            entity.Property(j => j.Title)
                  .IsRequired()
                  .HasMaxLength(120);

            entity.Property(j => j.Company)
                  .IsRequired()
                  .HasMaxLength(80);

            entity.Property(j => j.Location)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(j => j.Description)
                  .IsRequired()
                  .HasMaxLength(2000);

            // ── Enum storage ──────────────────────────────────────────
            // Store JobType as a string ("FullTime") rather than an integer (0).
            // Consistent with JsonStringEnumConverter in Program.cs.
            // Makes the database readable without looking up source code.
            entity.Property(j => j.Type)
                  .HasConversion<string>()
                  .IsRequired()
                  .HasMaxLength(20);

            // ── Decimal precision ─────────────────────────────────────
            entity.Property(j => j.SalaryMin)
                  .HasPrecision(18, 2);

            entity.Property(j => j.SalaryMax)
                  .HasPrecision(18, 2);

            // ── Unique index ──────────────────────────────────────────
            // Database-level enforcement of the idempotency rule.
            // DuplicateJobListingException enforces the same rule in
            // the application layer — this is the database-level safety net.
            // If two requests slip through at the same millisecond,
            // the database will reject the second one.
            entity.HasIndex(j => new { j.Title, j.Company })
                  .IsUnique()
                  .HasDatabaseName("ix_job_listings_title_company");
        });
    }
}
