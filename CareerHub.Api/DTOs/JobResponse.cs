using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// CHANGED FROM 2.1:
// - Company string removed (company data now comes from the related entity)
// - CompanyName added (projected from j.Company.Name in the query)
// - ApplicationCount added (computed by the database with COUNT(*), not in C#)

public record JobResponse(
    Guid id,
    string Title,
    string Description,
    string CompanyName,       // from Company entity — projected in the query
    string Location,
    JobType Type,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string SalaryDisplay,
    DateTime PostedAt,
    bool IsActive,
    int ApplicationCount      // COUNT(*) computed by the database
);
