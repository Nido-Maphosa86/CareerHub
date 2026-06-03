using System.ComponentModel.DataAnnotations;

namespace CareerHub.Api.DTOs;

// What the client sends to create a company
public record CreateCompanyRequest(
    [Required(ErrorMessage = "Company name is required")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    string Name,

    [MaxLength(500)] string? Website,
    [MaxLength(100)] string? Industry
);

// What the API returns after creating or fetching a company
public record CompanyResponse(
    Guid Id,
    string Name,
    string? Website,
    string? Industry
);
