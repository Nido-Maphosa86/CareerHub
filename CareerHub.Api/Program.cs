using System.Text.Json.Serialization;
using Scalar.AspNetCore;

// ════════════════════════════════════════════════════
// PHASE 1 — BUILDER: Register services into the
//           Dependency Injection container
// ════════════════════════════════════════════════════
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings ("FullTime") not integers (0).
        // A client reading "FullTime" understands it immediately.
        // A client reading 0 has to look up the source code.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();       // Register built-in OpenAPI document generation

builder.Services.AddProblemDetails(); // Enable the RFC 7807 standard error format.
                                      // Every error response will now be a consistent
                                      // JSON object with type, title, status and detail.

// ════════════════════════════════════════════════════
// TRANSITION — Build() seals the DI container.
// Nothing can be registered after this line.
// ════════════════════════════════════════════════════
var app = builder.Build();

// ════════════════════════════════════════════════════
// PHASE 2 — PIPELINE: Configure the middleware chain.
// Order matters. Every request passes through these
// in sequence, top to bottom.
// ════════════════════════════════════════════════════

app.UseExceptionHandler(); // Catch any unhandled exception that bubbles up and return
                           // a 500 Problem Details response instead of crashing the server.

app.UseStatusCodePages();  // Catch any 4xx/5xx response that has an empty body
                           // (e.g. a plain 404 from routing) and add a Problem Details body.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();             // Serves /openapi/v1.json
    app.MapScalarApiReference();  // Serves the Scalar UI at /scalar/v1
}

app.MapControllers(); // Activate attribute routing for all [ApiController] classes

app.Run(); // Start the Kestrel web server — blocks until the process exits
