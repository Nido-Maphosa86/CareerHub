using CareerHub.Api.Data;
using CareerHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerHub.Api.Services;

// Runs once at startup, then once every 24 hours.
// Finds every job listing where Status is Active but ClosingDate has already
// passed, sets Status to Closed and IsActive to false, and saves the change.
//
// Background services are registered as singletons - one instance lives for
// the whole life of the application. CareerHubDbContext is registered as
// scoped - it is only meant to live for the length of one request or one
// unit of work. A singleton cannot safely hold a scoped service directly,
// so instead we inject IServiceScopeFactory and open a new scope every time
// this service needs to talk to the database, then let that scope (and the
// DbContext inside it) get disposed straight after.
public class JobListingExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<JobListingExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job listing expiry service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireListingsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // A single failed run (for example, the database is briefly
                // unreachable) must not crash the whole application.
                // Log the error and simply try again on the next cycle.
                logger.LogError(ex, "Job listing expiry run failed. Will retry on the next cycle.");
            }

            // Task.Delay respects the cancellation token, so the service shuts
            // down cleanly as soon as the application stops, instead of
            // blocking shutdown for up to 24 hours.
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }

        logger.LogInformation("Job listing expiry service stopped.");
    }

    private async Task ExpireListingsAsync(CancellationToken cancellationToken)
    {
        // Open a fresh scope and resolve a scoped DbContext from it.
        // The "using" disposes the scope (and the DbContext) as soon as
        // this method returns.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerHubDbContext>();

        var now = DateTime.UtcNow;

        var expiredListings = await db.JobListings
            .Where(j => j.Status == JobListingStatus.Active && j.ClosingDate < now)
            .ToListAsync(cancellationToken);

        if (expiredListings.Count == 0)
        {
            logger.LogInformation("Job listing expiry run: nothing to close.");
            return;
        }

        foreach (var listing in expiredListings)
        {
            listing.Status   = JobListingStatus.Closed;
            listing.IsActive = false;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Job listing expiry run: closed {Count} listing(s) past their closing date.",
            expiredListings.Count);
    }
}
