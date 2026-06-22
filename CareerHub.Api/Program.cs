using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Data;
using CareerHub.Api.Infrastructure;
using CareerHub.Api.Infrastructure.OpenApi;
using CareerHub.Api.Middleware;
using CareerHub.Api.Services;

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

    // ── OPENAPI — Day 3 ──────────────────────────────────────────────────
    // AddDocumentTransformer registers CareerHubDocumentTransformer, which
    // fills in the title, description, contact info, and server list shown
    // on the Scalar docs page.
    builder.Services.AddOpenApi(options =>
        options.AddDocumentTransformer<CareerHubDocumentTransformer>());

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ── RESPONSE COMPRESSION — Day 3 ─────────────────────────────────────
    // application/json is added explicitly because the default MIME type
    // list used by ResponseCompressionDefaults does not include it, and
    // almost every response this API returns is JSON.
    // EnableForHttps is on because in production this API sits behind
    // HTTPS, and without it compression is skipped for HTTPS responses
    // by default (to avoid the BREACH attack surface on dynamic content
    // that reflects request data — not a concern here, this is a JSON API).
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Append("application/json");
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        options.Level = System.IO.Compression.CompressionLevel.Fastest);

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        options.Level = System.IO.Compression.CompressionLevel.Fastest);

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

    // ── HEALTH CHECKS — Day 3 ───────────────────────────────────────────────
    // AddDbContextCheck comes from the Microsoft.Extensions.Diagnostics.HealthChecks.
    // EntityFrameworkCore package. It runs a trivial query against CareerHubDbContext
    // to confirm the database is actually reachable, not just that the app is running.
    // Tagged "ready" so the readiness endpoint below can pick it out specifically.
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CareerHubDbContext>(
            name: "database",
            tags: ["ready"]);

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

    // ── BACKGROUND SERVICE — Day 4 ──────────────────────────────────────────
    // Runs at startup and then every 24 hours, closing job listings whose
    // ClosingDate has passed. See Services/JobListingExpiryService.cs.
    builder.Services.AddHostedService<JobListingExpiryService>();

    var app = builder.Build();
   app.UseCors("AllowFrontend");
    // ── MIDDLEWARE PIPELINE ────────────────────────────────────────────────
    // Order matters. Response compression goes first so every response below
    // it in the pipeline gets compressed before it reaches the client.
    // CORS before auth. Rate limiter after CORS but before auth.

    app.UseResponseCompression();

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

    // ── HEALTH CHECK ENDPOINTS — Day 3 ──────────────────────────────────────
    // /health/live — "is the process running and able to respond at all?"
    // Predicate = _ => false means no individual checks run here, so a slow
    // or unreachable database does not make liveness fail. An orchestrator
    // (Docker, Kubernetes) uses this to decide whether to restart the container.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // /health/ready — "is the app ready to actually serve real traffic?"
    // Runs every check tagged "ready" (currently just the database check).
    // A load balancer uses this to decide whether to send traffic to this
    // instance — if the database is down, this returns Unhealthy and traffic
    // is routed elsewhere instead of to an instance that can't serve requests.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

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