using CareerHub.Api.Repositories;
using CareerHub.Api.Services;

namespace CareerHub.Api.Infrastructure;

// ══════════════════════════════════════════════════════════════════════
// DI REGISTRATION — matches the class pattern from BookingFeature.
//
// Extension methods group registrations by feature so Program.cs stays flat.
// Program.cs calls these methods — it must NOT call AddScoped, AddTransient,
// or AddSingleton directly for any application service or repository.
//
// WHY Scoped for all services and repositories?
//   Every service and repository depends on CareerHubDbContext, which is Scoped
//   (one instance per HTTP request). Any class that holds a Scoped dependency
//   must itself be Scoped. A Singleton capturing a Scoped service is a bug that
//   .NET catches at startup with ValidateOnBuild — the application refuses to start.
//
// EVIDENCE: Deliberately registering JobListingService as Singleton produces:
//   "Cannot consume scoped service 'IJobListingRepository' from singleton 'IJobListingService'."
//   Fix: change AddSingleton → AddScoped.
// ══════════════════════════════════════════════════════════════════════

public static class ServiceCollectionExtensions
{
    // Registers all job listing repositories and services.
    // JobListingService depends on IJobListingRepository and ICompanyRepository.
    public static IServiceCollection AddJobListingFeature(
        this IServiceCollection services)
    {
        services.AddScoped<IJobListingRepository, JobListingRepository>();
        services.AddScoped<IJobListingService,    JobListingService>();
        return services;
    }

    // Registers all company repositories and services.
    public static IServiceCollection AddCompanyFeature(
        this IServiceCollection services)
    {
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyService,    CompanyService>();
        return services;
    }

    // Registers all application repositories and services.
    // ApplicationService depends on IApplicationRepository and IJobListingRepository.
    public static IServiceCollection AddApplicationFeature(
        this IServiceCollection services)
    {
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IApplicationService,    ApplicationService>();
        return services;
    }
}
