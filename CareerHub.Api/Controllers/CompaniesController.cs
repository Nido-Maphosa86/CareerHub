using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using CareerHub.Api.DTOs;
using CareerHub.Api.Services;

namespace CareerHub.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class CompaniesController(ICompanyService companyService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List all companies")]
    [EndpointDescription("Returns every company registered on CareerHub. No authentication required.")]
    public async Task<ActionResult<IEnumerable<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken ct) =>
        Ok(await companyService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a single company")]
    [EndpointDescription("Returns one company by id. No authentication required.")]
    public async Task<ActionResult<CompanyResponse>> GetCompanyByIdAsync(
        Guid id, CancellationToken ct) =>
        Ok(await companyService.GetByIdAsync(id, ct));

    [Authorize(Roles = "Employer")]
    [HttpPost]
    [EndpointSummary("Create a company")]
    [EndpointDescription(
        "Registers a new company on CareerHub. Requires the Employer role. " +
        "Company names must be unique.")]
    public async Task<ActionResult<CompanyResponse>> CreateCompanyAsync(
        [FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var response = await companyService.CreateAsync(request, ct);
        return Created($"/api/v1/companies/{response.Id}", response);
    }
}
