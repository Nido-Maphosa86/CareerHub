using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CareerHub.Api.Infrastructure;

// ══════════════════════════════════════════════════════════════════════
// SLOW QUERY INTERCEPTOR — Part 7 of Assignment 2.4
//
// Implements DbCommandInterceptor to measure every SQL command.
// When a command exceeds the configured threshold, it is logged at
// Warning level with the elapsed time and full SQL text.
//
// Registered as Singleton because it holds no request state —
// the same instance safely handles all requests concurrently.
// Both IConfiguration and ILogger<T> are Singleton-safe.
//
// To prove it works: set "SlowQueryThresholdMs": 0 in
// appsettings.Development.json and run GET /jobs — every query
// will appear as a warning in the terminal.
// ══════════════════════════════════════════════════════════════════════

public class SlowQueryInterceptor(
    IConfiguration configuration,
    ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    // Read threshold from configuration — defaults to 100ms if absent.
    // Reading in the constructor (not per-call) avoids repeated config lookups.
    private readonly int _thresholdMs = configuration.GetValue<int>("SlowQueryThresholdMs", 100);

    // ── Sync hook — called after a reader-returning command completes ──────
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogIfSlow(eventData.Duration, command.CommandText);
        return result;
    }

    // ── Async hook — called after an async reader-returning command completes ──
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(eventData.Duration, command.CommandText);
        return new ValueTask<DbDataReader>(result);
    }

    // ── Private helper ────────────────────────────────────────────────────

    private void LogIfSlow(TimeSpan duration, string sql)
    {
        if (duration.TotalMilliseconds > _thresholdMs)
        {
            // Log at Warning so it stands out in log aggregators.
            // ElapsedMs and Sql are separate structured fields — not concatenated strings.
            // A log aggregator can query: "show me all warnings where ElapsedMs > 500".
            logger.LogWarning(
                "Slow query detected: {ElapsedMs}ms\n{Sql}",
                (int)duration.TotalMilliseconds,
                sql);
        }
    }
}
