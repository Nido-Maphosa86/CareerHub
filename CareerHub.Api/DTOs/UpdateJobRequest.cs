using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// What the client sends to fully replace an existing job listing.
// Fields and validation rules are identical to CreateJobRequest.
// Kept as a separate type so the two can diverge in future
// (e.g. if we add partial PATCH support later).
// The ID always comes from the route — never from the body.
public record UpdateJobRequest(

    [Required(ErrorMessage = "Title is required")]
    [MinLength(5,   ErrorMessage = "Title must be at least 5 characters")]
    [MaxLength(120, ErrorMessage = "Title cannot exceed 120 characters")]
    string Title,

    [Required(ErrorMessage = "Company is required")]
    [MinLength(2,  ErrorMessage = "Company must be at least 2 characters")]
    [MaxLength(80, ErrorMessage = "Company cannot exceed 80 characters")]
    string Company,

    [Required(ErrorMessage = "Location is required")]
    string Location,

    [Required(ErrorMessage = "Description is required")]
    [MinLength(20, ErrorMessage = "Description must be at least 20 characters")]
    string Description,

    [Required(ErrorMessage = "Type is required — valid values: FullTime, PartTime, Contract, Internship")]
    JobType? Type,

    [Range(1, double.MaxValue, ErrorMessage = "SalaryMin must be greater than zero")]
    decimal? SalaryMin,

    [Range(1, double.MaxValue, ErrorMessage = "SalaryMax must be greater than zero")]
    decimal? SalaryMax

) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMax <= SalaryMin)
        {
            yield return new ValidationResult(
                "SalaryMax must be greater than SalaryMin.",
                [nameof(SalaryMax)]
            );
        }
    }
}
