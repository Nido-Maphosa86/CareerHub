using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Data;
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

    builder.Services.AddOpenApi();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Translates domain exceptions to Problem Details

    builder.Services.AddProblemDetails(); // RFC 7807 standardised error format

    // CORS — allows the Next.js frontend on port 3000 to call this API
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontEndPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // ── EF Core + PostgreSQL ──────────────────────────────────────────────
    // AddDbContext registers CareerHubDbContext as Scoped —
    // one instance per HTTP request. This is the correct lifetime
    // for a unit of work: all reads and writes in a single request
    // share the same DbContext and change tracker.
    // Connection string is read from appsettings.Development.json —
    // never hardcoded in source code.
    builder.Services.AddDbContext<CareerHubDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    );

    // JWT secret key — read from config, never hardcoded
    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = true,  // reject expired tokens
                ValidateIssuerSigningKey = true,  // verify the signature
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecretKey)
                )
            };
        });

    builder.Services.AddAuthorization(); // required for [Authorize(Roles = "...")] to work

    // ════════════════════════════════════════════════════
    // TRANSITION — Build() seals the DI container.
    // Nothing can be registered after this line.
    // ════════════════════════════════════════════════════
    var app = builder.Build();

    // ════════════════════════════════════════════════════
    // PHASE 2 — PIPELINE: Configure the middleware chain.
    // Order matters. Top to bottom.
    // ════════════════════════════════════════════════════

    app.UseSerilogRequestLogging(); // log every request — must be first

    app.UseCors("FrontEndPolicy");  // handle browser preflight before auth

    app.UseExceptionHandler();      // catch all thrown exceptions

    app.UseStatusCodePages();       // add body to empty error responses

    app.UseAuthentication();        // read the JWT, populate User

    app.UseAuthorization();         // check [Authorize] attributes

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();             // serves /openapi/v1.json
        app.MapScalarApiReference();  // serves the Scalar UI at /scalar/v1
    }

    app.MapControllers(); // activate attribute routing for all [ApiController] classes

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush(); // ensure all buffered log entries are written before exit
}
