using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// What the API returns to clients — the public contract.
// SalaryDisplay is computed at mapping time; it is never stored in the domain model.
// This is the clearest example of why a response DTO is not just a copy of the model.
public record JobResponse(
    Guid id,
    string Title,
    string Description,
    string Company,
    string Location,
    JobType Type,        // serialized as "FullTime" not 0 — configured in Program.cs
    decimal? SalaryMin,
    decimal? SalaryMax,
    string SalaryDisplay, // e.g. "R25,000 – R40,000/month" — computed, not stored
    DateTime PostedAt,
    bool IsActive
);
