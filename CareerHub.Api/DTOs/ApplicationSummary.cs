namespace CareerHub.Api.DTOs;

public record ApplicationSummary(
    string ApplicantName,
    DateTime SubmittedAt,
    string Status
);