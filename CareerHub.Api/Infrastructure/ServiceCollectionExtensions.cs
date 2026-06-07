using CareerHub.Api.Repositories;
using CareerHub.Api.Services;

namespace CareerHub.Api.Infrastructure;

// WHAT CHANGED FROM 2.3:
// - AddInfrastructure() method added — registers SlowQueryInterceptor as Singleton.
//   Singleton is correct because the interceptor holds no request state.
//   IConfiguration and ILogger<T> are both Singleton-safe to inject into it.

public static class ServiceCollectionExtensions
{
    // Registers infrastructure services — interceptors and cross-cutting concerns.
    // Called before AddDbContext so the interceptor is resolvable when DbContext is built.
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // Singleton: one instance for the entire application lifetime.
        // Safe because it carries no per-request state.
        services.AddSingleton<SlowQueryInterceptor>();
        return services;
    }

    public static IServiceCollection AddJobListingFeature(
        this IServiceCollection services)
    {
        services.AddScoped<IJobListingRepository, JobListingRepository>();
        services.AddScoped<IJobListingService,    JobListingService>();
        return services;
    }

    public static IServiceCollection AddCompanyFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyService,    CompanyService>();
        return services;
    }

    public static IServiceCollection AddApplicationFeature(
        this IServiceCollection services)
    {
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IApplicationService,    ApplicationService>();
        return services;
    }
}
