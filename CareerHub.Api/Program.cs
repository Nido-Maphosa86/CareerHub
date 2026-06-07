using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using CareerHub.Api.Data;
using CareerHub.Api.Infrastructure;
using CareerHub.Api.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting up the CareerHub API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // ══════════════════════════════════════════════════════════════════
    // BUILD-TIME DI VALIDATION — Part 5 of Assignment 2.3
    //
    // ValidateOnBuild: if the DI graph is invalid (e.g. a Singleton
    // captures a Scoped service), the app refuses to start and prints
    // the exact misconfiguration.
    //
    // Test: change AddJobListingFeature to use AddSingleton for
    // IJobListingService — you will see:
    //   "Cannot consume scoped service 'IJobListingRepository'
    //    from singleton 'IJobListingService'."
    // Fix: change it back to AddScoped.
    // ══════════════════════════════════════════════════════════════════
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes  = true;  // catch Scoped-in-Singleton at request time
        options.ValidateOnBuild = true;  // catch it at startup — fail fast
    });

    // ── PHASE 1: Register services ─────────────────────────────────────

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddCors(options =>
        options.AddPolicy("FrontEndPolicy", policy =>
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    builder.Services.AddDbContext<CareerHubDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

    // ── Feature registrations — no AddScoped/AddSingleton directly here ──
    // Program.cs calls extension methods. All individual registrations
    // live in Infrastructure/ServiceCollectionExtensions.cs.
    builder.Services
        .AddJobListingFeature()
        .AddCompanyFeature()
        .AddApplicationFeature();

    var app = builder.Build();

    // ── PHASE 2: Configure pipeline ────────────────────────────────────

    app.UseSerilogRequestLogging();
    app.UseCors("FrontEndPolicy");
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapControllers();
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
