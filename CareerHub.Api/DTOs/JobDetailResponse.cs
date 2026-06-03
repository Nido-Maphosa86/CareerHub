using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// Summary of a single application — returned inside JobDetailResponse.
// Only exposes what the job listing view needs — no applicant email for privacy.
public record ApplicationSummary(
    string ApplicantName,
    DateTime SubmittedAt,
    string Status
);

// Full detail response for GET /jobs/{id}.
// Includes the applications received and the name of each applicant.
// Separated from JobResponse so the list endpoint stays compact.
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
    IEnumerable<ApplicationSummary> Applications
);
