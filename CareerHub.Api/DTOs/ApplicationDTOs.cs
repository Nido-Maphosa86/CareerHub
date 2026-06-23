using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// What the candidate sends in the body when applying for a job.
// The job (listingId) comes from the URL and the applicant from the JWT,
// so neither appears here. Validation mirrors the frontend Zod schema so the
// server enforces the same rules even if the client is bypassed.
public class ApplyRequest : IValidatableObject
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string Email { get; init; } = string.Empty;

    // Optional. Null/absent is fine; a present value must look like a phone.
    [Phone(ErrorMessage = "Enter a valid phone number")]
    public string? Phone { get; init; }

    [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
    public int YearsOfExperience { get; init; }

    [Required(ErrorMessage = "Cover letter is required")]
    [StringLength(2000, MinimumLength = 50,
        ErrorMessage = "Cover letter must be between 50 and 2000 characters")]
    public string CoverLetter { get; init; } = string.Empty;

    // Optional. [Url] passes for null and only validates a present value.
    [Url(ErrorMessage = "Enter a valid URL")]
    public string? LinkedInUrl { get; init; }

    public bool AvailableImmediately { get; init; }

    [Range(0, 520, ErrorMessage = "Notice period must be between 0 and 520 weeks")]
    public int NoticePeriodWeeks { get; init; }

    // Cross-field rule — mirrors the Zod .refine on the frontend:
    // if not available immediately, notice period must be greater than zero.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AvailableImmediately && NoticePeriodWeeks <= 0)
        {
            yield return new ValidationResult(
                "Notice period must be greater than 0 if not available immediately.",
                [nameof(NoticePeriodWeeks)]);
        }
    }
}

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
