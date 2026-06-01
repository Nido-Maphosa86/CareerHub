using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

    builder.Services.AddOpenApi();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); // Translates domain exceptions to Problem Details

    builder.Services.AddProblemDetails(); // RFC 7807 standardised error format

    // CORS — allows the Next.js frontend on port 3000 to call this API.
    // Browsers block cross-origin requests by default; this opts the API in.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontEndPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Next.js dev port
                  .AllowAnyHeader()                     // allows Authorization, Content-Type, etc.
                  .AllowAnyMethod();                    // allows GET, POST, PUT, DELETE, etc.
        });
    });

    // JWT secret key — read from config, never hardcoded in source code
    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;

    // JWT Bearer authentication — validates the token on every protected request
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = false, // not checking who issued the token (our own API)
                ValidateAudience         = false, // not checking who the token is intended for
                ValidateLifetime         = true,  // reject expired tokens
                ValidateIssuerSigningKey = true,  // verify the signature matches our secret key
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecretKey)
                )
            };
        });

    builder.Services.AddAuthorization(); // required for [Authorize(Roles = "...")] to evaluate correctly

    // ════════════════════════════════════════════════════
    // TRANSITION — Build() seals the DI container.
    // Nothing can be registered after this line.
    // ════════════════════════════════════════════════════
    var app = builder.Build();

    // ════════════════════════════════════════════════════
    // PHASE 2 — PIPELINE: Configure the middleware chain.
    // Order matters. Top to bottom.
    // ════════════════════════════════════════════════════

    app.UseSerilogRequestLogging(); // Log every HTTP request — must be first so every request is captured

    app.UseCors("FrontEndPolicy");  // Must be early to intercept browser preflight OPTIONS requests
                                    // before authentication or exception handling runs

    app.UseAuthentication();        // Reads the JWT from the Authorization header and populates User
                                    // Must come before UseAuthorization

    app.UseAuthorization();         // Checks [Authorize] attributes — must come after UseAuthentication
                                    // You cannot check what someone is allowed to do before knowing who they are

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
