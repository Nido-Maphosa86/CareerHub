using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Middleware;

// ════════════════════════════════════════════════════
// Bootstrap Serilog before the host is built.
// This ensures even startup exceptions are logged.
// ════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting up the CareerHub API...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace the default .NET logger with Serilog
    builder.Host.UseSerilog();

    // ════════════════════════════════════════════════════
    // PHASE 1 — BUILDER: Register services
    // ════════════════════════════════════════════════════

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Serialize enums as strings ("FullTime") not integers (0)
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();      // Built-in OpenAPI document generation

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Typed exception handler — translates domain exceptions to Problem Details

    builder.Services.AddProblemDetails(); // RFC 7807 standardised error format

    // ════════════════════════════════════════════════════
    // TRANSITION — Build() seals the DI container.
    // Nothing can be registered after this line.
    // ════════════════════════════════════════════════════
    var app = builder.Build();

    // ════════════════════════════════════════════════════
    // PHASE 2 — PIPELINE: Configure the middleware chain.
    // Order matters. Top to bottom.
    // ════════════════════════════════════════════════════

    app.UseSerilogRequestLogging(); // Logs every HTTP request + final response automatically.
                                    // Must come BEFORE UseExceptionHandler so exceptions
                                    // are still caught and the request is logged correctly.

    app.UseExceptionHandler();      // Activates GlobalExceptionHandler — catches all thrown exceptions

    app.UseStatusCodePages();       // Fills empty 4xx/5xx responses with Problem Details body

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();             // Serves /openapi/v1.json
        app.MapScalarApiReference();  // Serves the Scalar UI at /scalar/v1
    }

    app.MapControllers(); // Activates attribute routing for all [ApiController] classes

    app.Run(); // Starts the Kestrel web server — blocks until the process exits
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush(); // Ensure all buffered log entries are written before the process exits
}
