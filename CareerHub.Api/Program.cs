using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Data;
using CareerHub.Api.Infrastructure;
using CareerHub.Api.Middleware;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

try
{
    Log.Information("Starting up the CareerHub API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes  = true;
        options.ValidateOnBuild = true;
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── CORS — Part 2 ─────────────────────────────────────────────────────
    // AllowAnyOrigin() combined with AllowCredentials() causes a startup exception:
    //   "The CORS protocol does not allow specifying a wildcard origin with credentials."
    // A wildcard origin (*) cannot be used with credentials because the browser cannot
    // send cookies or Authorization headers to an unknown origin — it is a security requirement.
    // We must explicitly list allowed origins instead.
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontEndPolicy", policy =>
            policy
                .WithOrigins(
                    "http://localhost:3000",          // Next.js dev server
                    "https://careerhub.vercel.app")   // production placeholder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()                   // required for Authorization header
                .WithExposedHeaders("X-Total-Count")); // frontend reads pagination total
    });

    // ── API VERSIONING — Part 6 ────────────────────────────────────────────
    // AssumeDefaultVersionWhenUnspecified = true means /api/jobs still works (non-breaking).
    // ReportApiVersions = true adds api-supported-versions header to every response.
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion                 = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions                 = true;
        })
        .AddMvc();

    // ── RATE LIMITING — Part 8 ─────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        // OnRejected: fired when any policy rejects a request.
        // Sets 429, writes Retry-After header, and returns a plain text body.
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                var seconds = (int)retryAfter.TotalSeconds;
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString();
                await context.HttpContext.Response.WriteAsync(
                    $"Rate limit exceeded. Please retry after {seconds} seconds.", token);
            }
            else
            {
                await context.HttpContext.Response.WriteAsync(
                    "Rate limit exceeded. Please try again later.", token);
            }
        };

        // Global policy — all endpoints via RequireRateLimiting on MapControllers
        options.AddFixedWindowLimiter("global", o =>
        {
            o.PermitLimit   = 200;
            o.Window        = TimeSpan.FromSeconds(60);
            o.QueueLimit    = 0; // reject immediately — no queue
        });

        // Search policy — sliding window prevents burst abuse of the GIN index
        // 6 segments = checked every 10 seconds within the 60 second window
        options.AddSlidingWindowLimiter("search", o =>
        {
            o.PermitLimit      = 30;
            o.Window           = TimeSpan.FromSeconds(60);
            o.SegmentsPerWindow = 6;
            o.QueueLimit       = 0;
        });

        // Apply policy — 60 minute window to prevent bot-driven fake applications
        // A 60-second window would be too short: legitimate users might apply to
        // several jobs in one session and hit the limit accidentally.
        options.AddFixedWindowLimiter("apply", o =>
        {
            o.PermitLimit = 5;
            o.Window      = TimeSpan.FromMinutes(60);
            o.QueueLimit  = 0;
        });

        // Post listing policy — employers don't need to post jobs rapidly
        options.AddFixedWindowLimiter("post-listing", o =>
        {
            o.PermitLimit = 10;
            o.Window      = TimeSpan.FromMinutes(60);
            o.QueueLimit  = 0;
        });
    });

    // ── EF CORE + INTERCEPTOR ──────────────────────────────────────────────
    builder.Services.AddInfrastructure();
    builder.Services.AddDbContext<CareerHubDbContext>((serviceProvider, options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
               .AddInterceptors(serviceProvider.GetRequiredService<SlowQueryInterceptor>()));

    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = false,
                ValidateAudience         = false,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecretKey))
            };
        });

    builder.Services.AddAuthorization();

    builder.Services
        .AddJobListingFeature()
        .AddCompanyFeature()
        .AddApplicationFeature();

    var app = builder.Build();

    // ── MIDDLEWARE PIPELINE ────────────────────────────────────────────────
    // Order matters. CORS before auth. Rate limiter after CORS but before auth.

    app.UseSerilogRequestLogging();
    app.UseCors("FrontEndPolicy");

    // Rate limiter must come after CORS so preflight OPTIONS requests are not rate-limited
    app.UseRateLimiter();

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Apply global rate limit to all controller endpoints
    app.MapControllers().RequireRateLimiting("global");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}
public partial class Program { }