using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// UPDATED in 2.3: added ClosingDate and Status
public record JobDetailResponse(
    Guid id,
    string Title,
    string Description,
    string CompanyName,
    string Location,
    JobType Type,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string SalaryDisplay,
    DateTime PostedAt,
    bool IsActive,
    IEnumerable<ApplicationSummary> Applications,
    DateTime ClosingDate,
    string Status
);