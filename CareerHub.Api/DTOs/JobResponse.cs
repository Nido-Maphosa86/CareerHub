using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// CHANGED FROM 2.2:
// - Status added — tells the client whether the listing is still accepting applications
// - ClosingDate added — lets the frontend show "Closes in X days"

public record JobResponse(
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
    int ApplicationCount,
    DateTime ClosingDate,
    string Status
);
