using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// What the API returns when fetching applications —
// includes enough context for both the employer dashboard and the applicant's own history
public record ApplicationResponse(
    Guid JobListingId,
    string JobTitle,
    string CompanyName,
    Guid ApplicantId,
    string ApplicantName,
    DateTime SubmittedAt,
    string Status
);

// What the employer sends when moving an application to a new status
public record UpdateApplicationStatusRequest(
    [Required(ErrorMessage = "Status is required")]
    ApplicationStatus Status
);
