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

    builder.Services.AddCors(options =>
        options.AddPolicy("FrontEndPolicy", policy =>
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader().AllowAnyMethod()));

    // Register the interceptor BEFORE AddDbContext so it is resolvable
    builder.Services.AddInfrastructure();

    // ── EF Core with interceptor wiring (Part 7) ─────────────────────────
    // The (serviceProvider, options) overload resolves SlowQueryInterceptor from DI.
    // AddInterceptors wires it into every command EF Core executes.
    builder.Services.AddDbContext<CareerHubDbContext>((serviceProvider, options) =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
               .AddInterceptors(serviceProvider.GetRequiredService<SlowQueryInterceptor>())
    );

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

    // Feature registrations — no AddScoped/AddSingleton directly here
    builder.Services
        .AddJobListingFeature()
        .AddCompanyFeature()
        .AddApplicationFeature();

    var app = builder.Build();

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
