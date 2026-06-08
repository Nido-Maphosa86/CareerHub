using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class CompaniesController(ICompanyService companyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken ct) =>
        Ok(await companyService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyResponse>> GetCompanyByIdAsync(
        Guid id, CancellationToken ct) =>
        Ok(await companyService.GetByIdAsync(id, ct));

    [Authorize(Roles = "Employer")]
    [HttpPost]
    public async Task<ActionResult<CompanyResponse>> CreateCompanyAsync(
        [FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var response = await companyService.CreateAsync(request, ct);
        return Created($"/api/v1/companies/{response.Id}", response);
    }
}
