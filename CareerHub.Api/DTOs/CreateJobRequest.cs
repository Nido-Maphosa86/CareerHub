using System.ComponentModel.DataAnnotations;
using CareerHub.Api.Models;

namespace CareerHub.Api.DTOs;

// CHANGED FROM 2.1:
// string Company → Guid? CompanyId (FK to companies table)
// The employer must first create or find their company,
// then reference it by ID when posting a job.

public record CreateJobRequest(

    [Required(ErrorMessage = "Title is required")]
    [MinLength(5,   ErrorMessage = "Title must be at least 5 characters")]
    [MaxLength(120, ErrorMessage = "Title cannot exceed 120 characters")]
    string Title,

    [Required(ErrorMessage = "CompanyId is required")]
    Guid? CompanyId,

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
